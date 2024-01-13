using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class FilesView : ViewBase<FilesViewModel>
{
    public FilesView() : base(initialize: false)
    {
        InitializeComponent();
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        DiscordService.Update("Files", "files");

        await ViewModel.LoadFiles();
    }

    private void OnFlatViewSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItems is null) return;

        ViewModel.SelectedExportItems = new ObservableCollection<FlatViewItem>(listBox.SelectedItems.OfType<FlatViewItem>());
    }

    private void OnTreeViewTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not TreeView treeView) return;
        if (treeView.SelectedItem is not TreeNodeItem item) return;
        ViewModel.SearchFilter = string.Empty;
        ViewModel.FlatViewJumpTo(item.PathInfo.Path);
    }

    private void OnFlatViewDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is not FlatViewItem item) return;
        ViewModel.TreeViewJumpTo(item.Path);
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        ViewModel.SelectedExportItems.Clear();
        ViewModel.SelectedFlatViewItem = null;
        ViewModel.SelectedTreeItem = null;
    }
}