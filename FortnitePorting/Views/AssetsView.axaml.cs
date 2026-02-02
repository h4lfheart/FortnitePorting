using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FortnitePorting.Controls.Navigation.Sidebar;
using FortnitePorting.Controls.WrapPanel;
using FortnitePorting.Framework;
using FortnitePorting.Models.Assets.Asset;
using FortnitePorting.Models.Assets.Custom;
using FortnitePorting.Models.Assets.Filters;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;
using BaseAssetItem = FortnitePorting.Models.Assets.Base.BaseAssetItem;

namespace FortnitePorting.Views;

public partial class AssetsView : ViewBase<AssetsViewModel>
{
    public AssetsView()
    {
        InitializeComponent();
        
        Navigation.Assets.Initialize(Sidebar);
        Navigation.Assets.AddBehaviorResolver<EExportType>(ChangeTab);
        Navigation.Assets.AddBehaviorResolver<string>(type =>
        {
            if (!Enum.TryParse(type, true, out EExportType enumType)) return;
            
            ChangeTab(enumType);
        });
        
        PointerWheelChangedEvent.AddClassHandler<TopLevel>((sender, args) =>
        {
            if ((args.KeyModifiers & KeyModifiers.Control) != 0)
            {
                var delta = args.Delta.Y;

                AppSettings.Application.AssetScale =
                    float.Clamp(AppSettings.Application.AssetScale + (delta > 0 ? 0.25f : -0.25f), 0.5f, 4.0f);
            
                args.Handled = true;
            }
        }, handledEventsToo: true);
    }

    private void ChangeTab(EExportType assetType)
    {
        if (ViewModel.AssetLoader.ActiveLoader?.Type == assetType) return;
        
        AssetsListBox.SelectedItems?.Clear();
        
        Discord.Update(assetType);
        var loaders = ViewModel.AssetLoader.Categories.SelectMany(category => category.Loaders);
        foreach (var loader in loaders)
        {
            if (loader.Type == assetType)
            {
                loader.Unpause();
            }
            else
            {
                loader.Pause();
            }
        }
        
        TaskService.Run(async () =>
        {
            await ViewModel.AssetLoader.Load(assetType);
        });
    }

    private void OnRandomButtonPressed(object? sender, RoutedEventArgs routedEventArgs)
    {
        AssetsListBox.SelectedIndex = Random.Shared.Next(0, AssetsListBox.Items.Count);
        
        if (AssetsListBox.SelectedItem is not AssetItem item) return;
        if (item.IconDisplayImage is not null) return;
        
        item.LoadBitmap();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItems is null) return;
        if (listBox.SelectedItems.Count == 0) return;
        
        ViewModel.AssetLoader.ActiveLoader.SelectedAssetInfos = [];
        foreach (var asset in listBox.SelectedItems.Cast<BaseAssetItem>())
        {
            if (asset is AssetItem assetItem)
            {
                
                ViewModel.AssetLoader.ActiveLoader.SelectedAssetInfos.Add(
                    ViewModel.AssetLoader.ActiveLoader.StyleDictionary.TryGetValue(asset.CreationData.DisplayName,
                        out var stylePaths)
                        ? new AssetInfo(assetItem, stylePaths.OrderBy(x => x))
                        : new AssetInfo(assetItem));
            }
            else if (asset is CustomAssetItem customAsset)
            {
                ViewModel.AssetLoader.ActiveLoader.SelectedAssetInfos.Add(new CustomAssetInfo(customAsset));
            }
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

    private void OnFilterChecked(object? sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox) return;
        if (checkBox.Content is null) return;
        if (checkBox.IsChecked is not { } isChecked) return;
        if (checkBox.DataContext is not FilterItem filterItem) return;

        ViewModel.AssetLoader.ActiveLoader.UpdateFilters(filterItem, isChecked);
    }

    private void OnItemSelected(object? sender, SidebarItemSelectedArgs e)
    {
        if (e.Tag is not EExportType assetType) return;
        
        ChangeTab(assetType);
    }

    private void OnItemRealized(object? sender, ItemRealizedEventArgs e)
    {
        if (e.Item is not AssetItem item) return;
        if (item.IconDisplayImage is not null) return;
        
        item.LoadBitmap();
    }
}