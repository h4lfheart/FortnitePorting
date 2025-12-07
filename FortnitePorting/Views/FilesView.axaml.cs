using Avalonia.Controls;
using Avalonia.Input;
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
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (sender is not TextBox textBox) return;

        if (ViewModel.UseFlatView)
            ViewModel.FlatSearchFilter = textBox.Text ?? string.Empty;
        else
            ViewModel.FileSearchFilter = textBox.Text ?? string.Empty;
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
        
        ViewModel.LoadFileItems(item);
        item.Expanded = true;
    }
    
    private void OnBreadcrumbItemPressed(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (args.Item is not TreeItem treeItem) return;
        
        ViewModel.ClearSearchFilter();
        ViewModel.LoadFileItems(treeItem);
    }

    private void OnTreeItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not TreeView treeView) return;
        if (treeView.SelectedItem is not TreeItem item) return;
        if (item.Type == ENodeType.File) return;
        
        ViewModel.LoadFileItems(item);
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
        
        ViewModel.ClearSearchFilter();
        ViewModel.FlatViewJumpTo(item.FilePath);
    }
    
    private void OnFlatItemDoubleTapped(object? sender, TappedEventArgs e)
    { 
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is not FlatItem item) return;
        
        ViewModel.FileViewJumpTo(item.Path);
    }

    private void OnItemRealized(object? sender, ItemRealizedEventArgs e)
    {
        if (e.Item is not TreeItem item) return;
        if (item.FileBitmap is not null) return;
        
        item.EnsureChildrenSorted();
        ViewModel.LoadFileBitmap(ref item);
    }
}