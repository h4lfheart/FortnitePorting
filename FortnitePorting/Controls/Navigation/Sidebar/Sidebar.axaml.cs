using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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

    private SidebarItemButton? _selectedButton;
    private bool _hasSelectedFirstButton;
    
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

        AttachedToVisualTree += async (sender, args) =>
        {
            if (_hasSelectedFirstButton) return;
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var firstValidButton = Enumerable
                    .OfType<SidebarItemButton>(Items)
                    .FirstOrDefault(b => b.Tag is not null);

                if (firstValidButton is not null)
                {
                    SelectButton(firstValidButton);
                    _hasSelectedFirstButton = true;
                }
            }, DispatcherPriority.Background);
        };
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
        
        if (_selectedButton is not null)
            _selectedButton.IsSelected = false;
        
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