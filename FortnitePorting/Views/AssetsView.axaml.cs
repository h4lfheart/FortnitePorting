using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FortnitePorting.Controls.Assets;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class AssetsView : ViewBase<AssetsViewModel>
{
    public AssetsView() : base(initialize: false)
    {
        InitializeComponent();
        ViewModel.ExpanderContainer = ExpanderContainer;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        DiscordService.Update(ViewModel.CurrentLoader?.Type ?? EAssetType.Outfit);
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItems is null) return;

        ViewModel.CurrentAssets.Clear();
        foreach (var asset in listBox.SelectedItems.Cast<AssetItem>()) ViewModel.CurrentAssets.Add(new AssetOptions(asset));
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


    private void OnFilterChecked(object? sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox) return;
        if (checkBox.Content is null) return;
        if (!checkBox.IsChecked.HasValue) return;

        ViewModel.ModifyFilters(checkBox.Content.ToString()!, checkBox.IsChecked.Value);
    }

    private void OnFilterClearClicked(object? sender, RoutedEventArgs e)
    {
        ViewModel.Filters.Clear();
        foreach (var child in FilterPopupPanel.Children)
        {
            if (child is not CheckBox checkBox) continue;
            checkBox.IsChecked = false;
        }
    }

    private void OnRandomButtonClicked(object? sender, RoutedEventArgs e)
    {
        AssetsListBox.SelectedIndex = Random.Shared.Next(0, ViewModel.ActiveCollection.Count);
    }

    private void OnSearchFilterChanged(object? sender, TextChangedEventArgs e)
    {
        ViewModel.SearchFilter = ViewModel.CurrentLoader?.SearchFilter ?? string.Empty;
    }
}