using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FortnitePorting.Controls.Assets;
using FortnitePorting.Models.Assets;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class AssetsView : ViewBase<AssetsViewModel>
{
    public AssetsView()
    {
        InitializeComponent();
    }

    private void OnRandomButtonPressed(object? sender, RoutedEventArgs routedEventArgs)
    {
        AssetsListBox.SelectedIndex = Random.Shared.Next(0, AssetsListBox.Items.Count);
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItems is null) return;

        ViewModel.SelectedAssets = [];
        foreach (var asset in listBox.SelectedItems.Cast<AssetItem>())
        {
            ViewModel.SelectedAssets.Add(new AssetInfo(asset));
        }
    }
    
    private void OnScrollAssets(object? sender, PointerWheelEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer) return;
        switch (e.Delta.Y)
        {
            case < 0:
                scrollViewer.LineLeft();
                break;
            case > 0:
                scrollViewer.LineRight();
                break;
        }
    }
}