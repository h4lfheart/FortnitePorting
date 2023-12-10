using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class FilesView : ViewBase<FilesViewModel>
{
    public FilesView() : base(initialize: false)
    {
        InitializeComponent();
        SearchTextBox.AddHandler(KeyDownEvent, OnSearchKeyDown, RoutingStrategies.Tunnel);
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

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
        ViewModel.FlatViewJumpTo(item.PathInfo.Path);
    }

    private void OnFlatViewDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is not FlatViewItem item) return;
        ViewModel.TreeViewJumpTo(item.Path);
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        if (e.Key != Key.Enter) return;
        
        ViewModel.SearchFilter = textBox.Text!;
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    { 
        if (sender is not TextBox textBox) return;
        var searchString = textBox.Text;
        
        if (!string.IsNullOrWhiteSpace(searchString)) return;

        ViewModel.SearchFilter = string.Empty;
    }
}