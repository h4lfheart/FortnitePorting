using System;
using System.Numerics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Lucdem.Avalonia.SourceGenerators.Attributes;
using Material.Icons;

namespace FortnitePorting.Controls.Navigation.Sidebar;

public partial class SidebarItemButton : UserControl, ISidebarItem
{
    [AvaDirectProperty] private string _text;
    [AvaDirectProperty] private MaterialIconKind? _icon;
    [AvaDirectProperty] private Bitmap? _iconBitmap;
    [AvaDirectProperty] private bool _isSelectable = true;
    [AvaDirectProperty] private Control? _footer;
    [AvaStyledProperty] private bool _isSelected = false;
    [AvaStyledProperty] private bool _isDragOver = false;
    [AvaStyledProperty] private bool _canReorder = false;
    
    public readonly RoutedEvent<RoutedEventArgs> ItemPressedEvent =
        RoutedEvent.Register<Sidebar, RoutedEventArgs>(
            nameof(ItemPressed),
            RoutingStrategies.Bubble);

    public event EventHandler ItemPressed
    {
        add => AddHandler(ItemPressedEvent, value);
        remove => RemoveHandler(ItemPressedEvent, value);
    }

    public bool ShouldShowIcon => IconBitmap is not null || Icon is not null;

    private Vector2? _dragStartPoint;
    private const double DragThreshold = 25.0;

    public SidebarItemButton()
    {
        InitializeComponent();
        
        DragDrop.SetAllowDrop(this, true);
        
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }
    
    public SidebarItemButton(string text = "", MaterialIconKind icon = MaterialIconKind.Palette, Bitmap? iconBitmap = null, object? tag = null) : this()
    {
        Text = text;
        Icon = icon;
        IconBitmap = iconBitmap;
        Tag = tag;
    }

    public void RaiseSelected()
    {
        var args = new RoutedEventArgs
        {
            RoutedEvent = ItemPressedEvent,
            Source = this
        };
        
        RaiseEvent(args);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!CanReorder) return;
        
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var point = e.GetPosition(this);
            _dragStartPoint = new Vector2((float)point.X, (float)point.Y);
        }
    }

    private async void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!CanReorder) return;
        
        if (_dragStartPoint.HasValue && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var currentPoint = e.GetPosition(this);
            var diff = new Vector2(
                (float)(currentPoint.X - _dragStartPoint.Value.X),
                (float)(currentPoint.Y - _dragStartPoint.Value.Y)
            );

            if (diff.Length() > DragThreshold)
            {
                var data = new DataObject();
                data.Set("SidebarItemButton", this);
                
                await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
                
                _dragStartPoint = null;
            }
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragStartPoint = null;
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (!CanReorder) return;
        
        if (e.Data.Contains("SidebarItemButton"))
        {
            var sourceButton = e.Data.Get("SidebarItemButton") as SidebarItemButton;
            if (sourceButton != this && sourceButton?.CanReorder == true)
            {
                e.DragEffects = DragDropEffects.Move;
                IsDragOver = true;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }
        
        e.Handled = true;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (!CanReorder) return;
        
        if (e.Data.Contains("SidebarItemButton"))
        {
            var sourceButton = e.Data.Get("SidebarItemButton") as SidebarItemButton;
            if (sourceButton != this && sourceButton?.CanReorder == true)
            {
                e.DragEffects = DragDropEffects.Move;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
        
        e.Handled = true;
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        IsDragOver = false;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        IsDragOver = false;
        
        if (!CanReorder) return;
        
        if (e.Data.Contains("SidebarItemButton"))
        {
            if (e.Data.Get("SidebarItemButton") is SidebarItemButton sourceButton && sourceButton != this && sourceButton.CanReorder)
            {
                RaiseEvent(new SidebarItemDragDropEventArgs(sourceButton, this)
                {
                    RoutedEvent = Sidebar.ItemDragDropEvent
                });
            }
        }
        
        e.Handled = true;
    }
}