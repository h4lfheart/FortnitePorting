using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class MeshesView : ViewBase<MeshesViewModel>
{
    public MeshesView() : base(lateInit: true)
    {
        InitializeComponent();
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        await ViewModel.LoadMeshes();
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
}