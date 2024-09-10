using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Models.Files;
using FortnitePorting.Shared.Framework;
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

        ViewModel.SearchFilter = textBox.Text ?? string.Empty;
    }
    
    private void OnTreeItemSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not TreeView treeView) return;
        if (treeView.SelectedItem is not TreeItem item) return;
        if (string.IsNullOrWhiteSpace(item.FilePath))
        {
            item.Expanded = !item.Expanded;
            return;
        }
        
        ViewModel.SearchFilter = string.Empty;
        ViewModel.FlatViewJumpTo(item.FilePath);
    }

    private void OnFlatItemSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is not FlatItem item) return;
        ViewModel.TreeViewJumpTo(item.Path);
    }
}