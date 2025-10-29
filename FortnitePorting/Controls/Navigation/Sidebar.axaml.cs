using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Lucdem.Avalonia.SourceGenerators.Attributes;

namespace FortnitePorting.Controls.Navigation;

public class SidebarItemSelectedArgs(object? tag) : RoutedEventArgs
{
    public object? Tag = tag;
}

public partial class Sidebar : UserControl
{
    [AvaDirectProperty] private Control _header;
    [AvaDirectProperty] private Control _footer;
    [AvaDirectProperty] private AvaloniaList<ISidebarItem> _items = [];

    private SidebarItemButton? _selectedButton;
    
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
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        SelectButton(Items.OfType<SidebarItemButton>().FirstOrDefault());
    }

    private void OnItemSelected(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Control control) return;
        if (control.FindAncestorOfType<SidebarItemButton>() is not { } button) return;
        
        SelectButton(button);
    }

    private void SelectButton(SidebarItemButton? button)
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