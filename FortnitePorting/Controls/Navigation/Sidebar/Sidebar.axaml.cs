using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Lucdem.Avalonia.SourceGenerators.Attributes;

namespace FortnitePorting.Controls.Navigation.Sidebar;

public class SidebarItemSelectedArgs(object? tag) : RoutedEventArgs
{
    public object? Tag = tag;
}

public class SidebarItemDragDropEventArgs : RoutedEventArgs
{
    public SidebarItemButton SourceButton { get; }
    public SidebarItemButton TargetButton { get; }

    public SidebarItemDragDropEventArgs(SidebarItemButton sourceButton, SidebarItemButton targetButton)
    {
        SourceButton = sourceButton;
        TargetButton = targetButton;
    }
}

public partial class Sidebar : UserControl
{
    [AvaDirectProperty] private Control _header;
    [AvaDirectProperty] private Control _footer;
    [AvaDirectProperty] private ObservableCollection<ISidebarItem> _items = [];
    [AvaDirectProperty] private bool _autoSelectDefault = true;

    public static readonly StyledProperty<ObservableCollection<ISidebarItem>> FlattenedItemsProperty =
        AvaloniaProperty.Register<Sidebar, ObservableCollection<ISidebarItem>>(nameof(FlattenedItems), new ObservableCollection<ISidebarItem>());

    public ObservableCollection<ISidebarItem> FlattenedItems
    {
        get => GetValue(FlattenedItemsProperty);
        private set => SetValue(FlattenedItemsProperty, value);
    }

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<Sidebar, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    private SidebarItemButton? _selectedButton;
    private bool _hasSelectedFirstButton;
    private bool _isUpdatingSelection;
    private readonly Dictionary<SidebarItemsSource, EventHandler> _itemsSourceSubscriptions = new();
    private readonly Dictionary<SidebarItemButton, (SidebarItemsSource itemsSource, object dataItem)> _buttonToDataMap = new();

    public readonly RoutedEvent<SidebarItemSelectedArgs> ItemSelectedEvent =
        RoutedEvent.Register<Sidebar, SidebarItemSelectedArgs>(
            nameof(ItemSelected),
            RoutingStrategies.Bubble);

    public event EventHandler<SidebarItemSelectedArgs> ItemSelected
    {
        add => AddHandler(ItemSelectedEvent, value);
        remove => RemoveHandler(ItemSelectedEvent, value);
    }

    public static readonly RoutedEvent<SidebarItemDragDropEventArgs> ItemDragDropEvent =
        RoutedEvent.Register<Sidebar, SidebarItemDragDropEventArgs>(
            nameof(ItemDragDrop),
            RoutingStrategies.Bubble);

    public event EventHandler<SidebarItemDragDropEventArgs> ItemDragDrop
    {
        add => AddHandler(ItemDragDropEvent, value);
        remove => RemoveHandler(ItemDragDropEvent, value);
    }

    public Sidebar()
    {
        InitializeComponent();

        Items.CollectionChanged += OnItemsCollectionChanged;
        this.GetObservable(SelectedItemProperty).Subscribe(OnSelectedItemChanged);
        AddHandler(ItemDragDropEvent, OnItemDragDropInternal);

        AttachedToVisualTree += async (sender, args) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_hasSelectedFirstButton) return;

                var firstValidButton = GetAllButtons().FirstOrDefault(b => b.Tag is not null);
                if (firstValidButton is not null)
                {
                    if (AutoSelectDefault)
                        SelectButton(firstValidButton);
                    _hasSelectedFirstButton = true;
                }
            }, DispatcherPriority.Background);
        };

        RebuildFlattenedItems();
    }

   

    public IEnumerable<SidebarItemButton> GetAllButtons()
    {
        foreach (var item in FlattenedItems)
        {
            switch (item)
            {
                case SidebarItemButton button:
                    yield return button;
                    break;
                
                case SidebarItemGroup group:
                {
                    foreach (var child in group.Items.OfType<SidebarItemButton>())
                        yield return child;
                    break;
                }
            }
        }
    }

    private void OnItemDragDropInternal(object? sender, SidebarItemDragDropEventArgs e)
    {
        var sourceButton = e.SourceButton;
        var targetButton = e.TargetButton;

        if (!_buttonToDataMap.TryGetValue(sourceButton, out var sourceInfo))
            return;

        if (!_buttonToDataMap.TryGetValue(targetButton, out var targetInfo))
            return;

        if (sourceInfo.itemsSource != targetInfo.itemsSource)
            return;

        var itemsSource = sourceInfo.itemsSource.ItemsSource;
        if (itemsSource is null)
            return;

        if (itemsSource is ObservableCollection<object> observableCollection)
        {
            var sourceIndex = observableCollection.IndexOf(sourceInfo.dataItem);
            var targetIndex = observableCollection.IndexOf(targetInfo.dataItem);

            if (sourceIndex != -1 && targetIndex != -1)
                observableCollection.Move(sourceIndex, targetIndex);
        }
        else if (itemsSource is IList list)
        {
            var sourceIndex = list.IndexOf(sourceInfo.dataItem);
            var targetIndex = list.IndexOf(targetInfo.dataItem);

            if (sourceIndex != -1 && targetIndex != -1)
            {
                list.RemoveAt(sourceIndex);
                list.Insert(targetIndex, sourceInfo.dataItem);
            }
        }
    }

    private void OnSelectedItemChanged(object? newValue)
    {
        if (_isUpdatingSelection) return;

        var button = GetAllButtons().FirstOrDefault(b => b.Tag?.Equals(newValue) == true);

        if (button is null) return;

        _isUpdatingSelection = true;
        SelectButtonInternal(button);
        _isUpdatingSelection = false;
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildFlattenedItems();
    }

    private void RebuildFlattenedItems()
    {
        var flattened = new ObservableCollection<ISidebarItem>();
        SidebarItemButton? newSelectedButton = null;

        _buttonToDataMap.Clear();

        foreach (var item in Items)
        {
            if (item is not SidebarItemsSource itemsSource)
            {
                if (item is SidebarItemGroup group)
                {
                    foreach (var child in group.Items.OfType<SidebarItemButton>())
                    {
                        if (child.Tag is not null && child.Tag.Equals(SelectedItem))
                            newSelectedButton = child;
                    }
                }

                flattened.Add(item);
                continue;
            }

            SubscribeToItemsSource(itemsSource);

            if (itemsSource.ItemsSource is null)
                continue;

            foreach (var (sidebarItem, button) in ResolveItems(itemsSource))
            {
                if (button is not null)
                {
                    button.CanReorder = itemsSource.CanReorder;
                    _buttonToDataMap[button] = (itemsSource, button.DataContext!);

                    if (button.Tag is not null && button.Tag.Equals(SelectedItem))
                        newSelectedButton = button;
                }

                flattened.Add(sidebarItem);
            }
        }

        FlattenedItems = flattened;

        if (newSelectedButton is not null)
        {
            _selectedButton = newSelectedButton;
            _selectedButton.IsSelected = true;
        }
    }

    private IEnumerable<(ISidebarItem SidebarItem, SidebarItemButton? Button)> ResolveItems(SidebarItemsSource source)
    {
        foreach (var dataItem in source.ItemsSource!)
        {
            if (source.ItemTemplate is not null)
            {
                var control = source.ItemTemplate.Build(dataItem);
                if (control is not ISidebarItem sidebarItem) continue;

                control.DataContext = dataItem;
                yield return (sidebarItem, control as SidebarItemButton);
            }
            else
            {
                if (dataItem is not ISidebarItem sidebarItem) continue;
                yield return (sidebarItem, dataItem as SidebarItemButton);
            }
        }
    }

    private void SubscribeToItemsSource(SidebarItemsSource itemsSource)
    {
        if (_itemsSourceSubscriptions.ContainsKey(itemsSource))
            return;

        EventHandler handler = (sender, args) => RebuildFlattenedItems();

        itemsSource.ItemsChanged += handler;
        _itemsSourceSubscriptions[itemsSource] = handler;
    }

    private void OnItemSelected(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Control control) return;
        if (control.FindAncestorOfType<SidebarItemButton>() is not { } button) return;

        if (!button.IsSelectable)
            button.RaiseSelected();
        else
            SelectButton(button);
    }

    public void SelectButton(SidebarItemButton? button, bool raiseEvent = true)
    {
        if (button is null) return;

        _isUpdatingSelection = true;
        SelectButtonInternal(button, raiseEvent);
        SelectedItem = button.Tag;
        _isUpdatingSelection = false;
    }

    private void SelectButtonInternal(SidebarItemButton? button, bool raiseEvent = true)
    {
        if (button is null) return;

        _selectedButton?.IsSelected = false;

        _selectedButton = button;
        _selectedButton.IsSelected = true;

        if (raiseEvent)
        {
            var args = new SidebarItemSelectedArgs(button.Tag)
            {
                RoutedEvent = ItemSelectedEvent,
                Source = this
            };

            RaiseEvent(args);
        }
    }
}