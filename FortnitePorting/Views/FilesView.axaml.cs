using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Models.Files;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class FilesView : ViewBase<FilesViewModel>
{
    public FilesView() : base()
    {
        InitializeComponent();
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (sender is not TextBox textBox) return;

        ViewModel.SearchFilter = textBox.Text ?? string.Empty;
    }

    private void OnFlatItemDoubleTapped(object? sender, TappedEventArgs e)
    { 
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is not FlatItem item) return;
        ViewModel.TreeViewJumpTo(item.Path);
    }

    private void OnTreeItemTapped(object? sender, TappedEventArgs e)
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

    private void OnGameNameChecked(object? sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox) return;
        if (checkBox.DataContext is not FileGameFilter fileGameFilter) return;
        if (checkBox.IsChecked is not { } isChecked) return;
        if (isChecked == fileGameFilter.IsChecked) return;
        
        if (isChecked)
            ViewModel.SelectedGameNames.Add(fileGameFilter.SearchName);
        else
            ViewModel.SelectedGameNames.Remove(fileGameFilter.SearchName);

        ViewModel.FakeRefreshFilters();
    }
}