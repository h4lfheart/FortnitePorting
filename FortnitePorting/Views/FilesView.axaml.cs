using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Controls.WrapPanel;
using FortnitePorting.Framework;
using FortnitePorting.Models.Files;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class FilesView : ViewBase<FilesViewModel>
{
    public FilesView() : base(FilesVM, initializeViewModel: false)
    {
        InitializeComponent();
        
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);

    }
    
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel.Context.UseFlatView) return;

        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsXButton1Pressed && ViewModel.Context.CanGoBack)
        {
            ViewModel.Context.GoBack();
            e.Handled = true;
        }
        else if (point.Properties.IsXButton2Pressed && ViewModel.Context.CanGoForward)
        {
            ViewModel.Context.GoForward();
            e.Handled = true;
        }
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (sender is not TextBox textBox) return;

        if (ViewModel.Context.UseFlatView)
            ViewModel.Context.FlatSearchFilter = textBox.Text ?? string.Empty;
        else
            ViewModel.Context.FileSearchFilter = textBox.Text ?? string.Empty;
    }

    private void OnFileItemDoubleTapped(object? sender, TappedEventArgs e)
    { 
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is not TreeItem item) return;
        if (item.Type == ENodeType.File)
        {
            TaskService.RunDispatcher(async () => await ViewModel.Preview());
            return;
        }
        
        ViewModel.Context.LoadFileItems(item);
        item.Expanded = true;
    }
    
    private void OnBreadcrumbItemPressed(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (args.Item is not TreeItem treeItem) return;
        
        ViewModel.Context.ClearSearchFilter();
        ViewModel.Context.LoadFileItems(treeItem);
    }

    private void OnTreeItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not TreeView treeView) return;
        if (treeView.SelectedItem is not TreeItem item) return;
        if (item.Type == ENodeType.File) return;
        
        ViewModel.Context.LoadFileItems(item);
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
        
        ViewModel.Context.ClearSearchFilter();
        ViewModel.Context.FlatViewJumpTo(item.FilePath);
    }
    
    private void OnFlatItemDoubleTapped(object? sender, TappedEventArgs e)
    { 
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is not FlatItem item) return;
        
        ViewModel.Context.FileViewJumpTo(item.Path);
    }

    private void OnItemRealized(object? sender, ItemRealizedEventArgs e)
    {
        if (e.Item is not TreeItem item) return;
        if (item.FileBitmap is not null) return;
        
        ViewModel.Context.RealizeFileData(item);
    }

    private void OnFlatViewHyperlinkPressed(object? sender, PointerPressedEventArgs e)
    {
        var searchTerm = ViewModel.Context.FileSearchFilter;
        
        ViewModel.Context.FileSearchFilter = string.Empty;
        ViewModel.Context.FileSearchText = string.Empty;
        
        ViewModel.Context.UseFlatView = true;
        ViewModel.Context.FlatSearchFilter = searchTerm;
        ViewModel.Context.FlatSearchText = searchTerm;
    }
}