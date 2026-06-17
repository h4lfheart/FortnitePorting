using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using FortnitePorting.Controls.Navigation.Sidebar;
using FortnitePorting.Controls.WrapPanel;
using FortnitePorting.Framework;
using FortnitePorting.Models.Assets;
using FortnitePorting.Models.Assets.Asset;
using FortnitePorting.Models.Assets.Custom;
using FortnitePorting.Models.Assets.Filters;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;
using Newtonsoft.Json;
using BaseAssetItem = FortnitePorting.Models.Assets.Base.BaseAssetItem;

namespace FortnitePorting.Views;

public partial class AssetsView : ViewBase<AssetsViewModel>
{
    private bool _finishedFirstLoad = false;
    
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
        
        AssetsListBox.AddHandler(PointerPressedEvent, OnAssetItemPressed, RoutingStrategies.Tunnel);
        AssetsListBox.AddHandler(PointerMovedEvent, OnAssetItemPointerMoved, RoutingStrategies.Tunnel);
        AssetsListBox.AddHandler(PointerReleasedEvent, OnAssetItemPointerReleased, RoutingStrategies.Tunnel);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (!_finishedFirstLoad)
        {
            Navigation.Assets.Open(AppSettings.Application.UseDefaultExportLoadType ? AppSettings.Application.DefaultExportLoadType : EExportType.Outfit);
            _finishedFirstLoad = true;
        }
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

        TaskService.Run(item.LoadBitmap);
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
                var stylePaths = 
                    ViewModel.AssetLoader.ActiveLoader.StyleDictionary.GetValueOrDefault(asset.CreationData.DisplayName) ??
                    ViewModel.AssetLoader.ActiveLoader.StyleDictionary.GetValueOrDefault(asset.CreationData.ID);
                
                ViewModel.AssetLoader.ActiveLoader.SelectedAssetInfos.Add(
                    stylePaths is not null
                        ? new AssetInfo(assetItem, stylePaths.OrderBy(x => x.EndsWith(asset.CreationData.ID, StringComparison.OrdinalIgnoreCase) ? 0 : 1))
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

    private void OnStyleBoxPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        if (control.DataContext is not AssetStyleInfo assetStyleInfo) return;
        if (assetStyleInfo.RequiredSelection) return;
        
        assetStyleInfo.SelectedStyleIndex = -1;
        assetStyleInfo.SelectedItems.Clear();
    }

    private PointerPressedEventArgs? _assetDragArgs;
    private Point _dragStartPosition;

    private void OnAssetItemPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Control source) return;
    
        var item = GetAssetItemFromSource(source);
        if (item is null) return;
    
        _assetDragArgs = e;
        _dragStartPosition = e.GetPosition(AssetsListBox);
    }

    private void OnAssetItemPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_assetDragArgs is null) return;

        var pos = e.GetPosition(AssetsListBox);
        if (Math.Abs(pos.X - _dragStartPosition.X) < 8 &&
            Math.Abs(pos.Y - _dragStartPosition.Y) < 8) return;

        var args = _assetDragArgs;
        _assetDragArgs = null;

        TaskService.RunDispatcher(async () => await StartDragAsync(args));
    }

    private void OnAssetItemPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _assetDragArgs = null;
    }

    private AssetItem? GetAssetItemFromSource(Control source)
    {
        var control = source;
        while (control is not null)
        {
            if (control.DataContext is AssetItem assetItem)
                return assetItem;
            control = control.Parent as Control;
        }
        return null;
    }
    
    private async Task StartDragAsync(PointerEventArgs e)
    {
        var dragDropInfoFile = Path.Combine(App.DataFolder.FullName, "info.fp_drag_drop");

        await File.WriteAllTextAsync(dragDropInfoFile, JsonConvert.SerializeObject(new
        {
            Paths = ViewModel.AssetLoader.ActiveLoader!.SelectedAssetInfos
                .OfType<AssetInfo>()
                .Select(asset => asset.Asset.CreationData.Object.GetPathName())
                .ToList()
        }));

        TaskService.Run(ExportClient.DiscoverAsync);

        var storageFile = await TopLevel.GetTopLevel(this)!
            .StorageProvider
            .TryGetFileFromPathAsync(new Uri(dragDropInfoFile));
        
        if (storageFile is null) return;
        
        var data = new DataObject();
        data.Set(DataFormats.Files, new[] { storageFile });

        await DragDrop.DoDragDrop(e, data, DragDropEffects.Copy | DragDropEffects.Move);
         
    }
}