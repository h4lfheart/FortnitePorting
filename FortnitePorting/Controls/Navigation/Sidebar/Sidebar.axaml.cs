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

public partial class Sidebar : UserControl
{
    [AvaDirectProperty] private Control _header;
    [AvaDirectProperty] private Control _footer;
    [AvaDirectProperty] private ObservableCollection<ISidebarItem> _items = [];

    public static readonly StyledProperty<ObservableCollection<ISidebarItem>> FlattenedItemsProperty =
        AvaloniaProperty.Register<Sidebar, ObservableCollection<ISidebarItem>>(nameof(FlattenedItems), new ObservableCollection<ISidebarItem>());

    public ObservableCollection<ISidebarItem> FlattenedItems
    {
        get => GetValue(FlattenedItemsProperty);
        private set => SetValue(FlattenedItemsProperty, value);
    }

    // Add bindable SelectedItem property
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
    
    public readonly RoutedEvent<SidebarItemSelectedArgs> ItemSelectedEvent =
        RoutedEvent.Register<Sidebar, SidebarItemSelectedArgs>(
            nameof(ItemSelected),
            RoutingStrategies.Bubble);

    public event EventHandler<SidebarItemSelectedArgs> ItemSelected
    {
        add => AddHandler(ItemSelectedEvent, value);
        remove => RemoveHandler(ItemSelectedEvent, value);
    }
    
    public Sidebar()
    {
        InitializeComponent();

        Items.CollectionChanged += OnItemsCollectionChanged;

        this.GetObservable(SelectedItemProperty).Subscribe(OnSelectedItemChanged);

        AttachedToVisualTree += async (sender, args) =>
        {
            if (_hasSelectedFirstButton) return;
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var firstValidButton = FlattenedItems
                    .OfType<SidebarItemButton>()
                    .FirstOrDefault(b => b.Tag is not null);

                if (firstValidButton is not null)
                {
                    SelectButton(firstValidButton);
                    _hasSelectedFirstButton = true;
                }
            }, DispatcherPriority.Background);
        };

        RebuildFlattenedItems();
    }

    private void OnSelectedItemChanged(object? newValue)
    {
        if (_isUpdatingSelection) return;
        
        var button = FlattenedItems
            .OfType<SidebarItemButton>()
            .FirstOrDefault(b => b.Tag?.Equals(newValue) == true);

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

        foreach (var item in Items)
        {
            if (item is SidebarItemsSource itemsSource)
            {
                SubscribeToItemsSource(itemsSource);
                
                if (itemsSource.ItemsSource != null && itemsSource.ItemTemplate != null)
                {
                    foreach (var dataItem in itemsSource.ItemsSource)
                    {
                        var control = itemsSource.ItemTemplate.Build(dataItem);
                        if (control is not ISidebarItem sidebarItem) continue;
                        
                        control.DataContext = dataItem;
                        
                        if (control is SidebarItemButton { Tag: not null } button && 
                            button.Tag.Equals(SelectedItem))
                        {
                            newSelectedButton = button;
                        }
                            
                        flattened.Add(sidebarItem);
                    }
                }
            }
            else
            {
                flattened.Add(item);
            }
        }

        FlattenedItems = flattened;

        if (newSelectedButton is null) return;
        
        _selectedButton = newSelectedButton;
        _selectedButton.IsSelected = true;
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
        
        SelectButton(button);
    }

    public void SelectButton(SidebarItemButton? button)
    {
        if (button is null) return;

        _isUpdatingSelection = true;
        SelectButtonInternal(button);
        SelectedItem = button.Tag;
        _isUpdatingSelection = false;
    }

    private void SelectButtonInternal(SidebarItemButton? button)
    {
        if (button is null) return;

        _selectedButton?.IsSelected = false;

        _selectedButton = button;
        _selectedButton.IsSelected = true;
        
        var args = new SidebarItemSelectedArgs(button.Tag)
        {
            RoutedEvent = ItemSelectedEvent,
            Source = this
        };
        
        RaiseEvent(args);
    }
}