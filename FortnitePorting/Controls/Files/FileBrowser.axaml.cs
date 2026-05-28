using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Controls.WrapPanel;
using FortnitePorting.Models.Files;
using FortnitePorting.Services;
using Lucdem.Avalonia.SourceGenerators.Attributes;

namespace FortnitePorting.Controls.Files;

public partial class FileBrowser : UserControl
{
    [AvaDirectProperty] private FileBrowserContext _context;
    
    public event Action<TreeItem>? FileItemDoubleTapped;
    
    public FileBrowser()
    {
        InitializeComponent();
        
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
    }
    
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Context.UseFlatView) return;

        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsXButton1Pressed && Context.CanGoBack)
        {
            Context.GoBack();
            e.Handled = true;
        }
        else if (point.Properties.IsXButton2Pressed && Context.CanGoForward)
        {
            Context.GoForward();
            e.Handled = true;
        }
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (sender is not TextBox textBox) return;

        if (Context.UseFlatView)
            Context.FlatSearchFilter = textBox.Text ?? string.Empty;
        else
            Context.FileSearchFilter = textBox.Text ?? string.Empty;
    }

    private void OnFileItemDoubleTapped(object? sender, TappedEventArgs e)
    { 
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is not TreeItem item) return;
        
        FileItemDoubleTapped?.Invoke(item);
        if (item.Type == ENodeType.Folder)
        {
            Context.LoadFileItems(item);
            item.Expanded = true;
        }
    }
    
    private void OnBreadcrumbItemPressed(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (args.Item is not TreeItem treeItem) return;
        
        Context.ClearSearchFilter();
        Context.LoadFileItems(treeItem);
    }

    private void OnTreeItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not TreeView treeView) return;
        if (treeView.SelectedItem is not TreeItem item) return;
        if (item.Type == ENodeType.File) return;
        
        Context.LoadFileItems(item);
    }
    
    private void OnTreeItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not TreeView treeView) return;
        if (treeView.SelectedItem is not TreeItem item) return;
        if (item.Type == ENodeType.Folder)
        {
            item.Expanded = !item.Expanded;
            return;
        }
        
        Context.ClearSearchFilter();
        Context.FlatViewJumpTo(item.FilePath);
    }
    
    private void OnFlatItemDoubleTapped(object? sender, TappedEventArgs e)
    { 
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is not FlatItem item) return;
        
        Context.FileViewJumpTo(item.Path);
    }

    private void OnItemRealized(object? sender, ItemRealizedEventArgs e)
    {
        if (e.Item is not TreeItem item) return;
        if (item.FileBitmap is not null) return;
        
        Context.RealizeFileData(item);
    }

    private void OnFlatViewHyperlinkPressed(object? sender, PointerPressedEventArgs e)
    {
        var searchTerm = Context.FileSearchFilter;
        
        Context.FileSearchFilter = string.Empty;
        Context.FileSearchText = string.Empty;
        
        Context.UseFlatView = true;
        Context.FlatSearchFilter = searchTerm;
        Context.FlatSearchText = searchTerm;
    }
}