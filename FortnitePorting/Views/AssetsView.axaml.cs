using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
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
    private bool _suppressSelectionChange;
    private PointerPressedEventArgs? _assetDragArgs;
    private Point _dragStartPosition;

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
            if ((args.KeyModifiers & KeyModifiers.Control) == 0) return;

            var delta = args.Delta.Y;
            AppSettings.Application.AssetScale =
                float.Clamp(AppSettings.Application.AssetScale + (delta > 0 ? 0.25f : -0.25f), 0.5f, 4.0f);

            args.Handled = true;
        }, handledEventsToo: true);

        AssetsListBox.AddHandler(PointerPressedEvent, OnAssetItemPressed, RoutingStrategies.Tunnel);
        AssetsListBox.AddHandler(PointerMovedEvent, OnAssetItemPointerMoved, RoutingStrategies.Tunnel);
        AssetsListBox.AddHandler(PointerReleasedEvent, OnAssetItemPointerReleased, RoutingStrategies.Tunnel);
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
                loader.Unpause();
            else
                loader.Pause();
        }

        TaskService.Run(async () => await ViewModel.AssetLoader.Load(assetType));
    }

    private void OnRandomButtonPressed(object? sender, RoutedEventArgs routedEventArgs)
    {
        AssetsListBox.SelectedIndex = Random.Shared.Next(0, AssetsListBox.Items.Count);

        if (AssetsListBox.SelectedItem is not AssetItem item) return;
        if (item.IconDisplayImage is not null) return;

        TaskService.Run(item.LoadBitmapAsync);
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItems is null || listBox.SelectedItems.Count == 0) return;

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
            case < 0: scrollViewer.LineLeft(); break;
            case > 0: scrollViewer.LineRight(); break;
        }
    }

    private void OnFilterChecked(object? sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox { Content: not null, IsChecked: { } isChecked, DataContext: FilterItem filterItem }) return;

        ViewModel.AssetLoader.ActiveLoader.UpdateFilters(filterItem, isChecked);
    }

    private void OnItemSelected(object? sender, SidebarItemSelectedArgs e)
    {
        if (e.Tag is not EExportType assetType) return;

        ChangeTab(assetType);
    }

    private void OnItemRealized(object? sender, ItemRealizedEventArgs e)
    {
        if (e.Item is not AssetItem { IconDisplayImage: null } item) return;

        TaskService.Run(item.LoadBitmapAsync);
    }

    private void OnStyleBoxPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { DataContext: AssetStyleInfo { RequiredSelection: false } assetStyleInfo }) return;

        assetStyleInfo.SelectedStyleIndex = -1;
        assetStyleInfo.SelectedItems.Clear();
    }

    private void OnStyleFlyoutItemPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (e.InitialPressMouseButton != MouseButton.Left) return;
        if (e.Source is not Control source || source.FindAncestorOfType<ListBoxItem>() is null) return;

        if (listBox.FindAncestorOfType<FlyoutPresenter>()?.Parent is Popup popup)
            popup.IsOpen = false;
    }

    private void OnAssetItemPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Control { DataContext: AssetItem item }) return;

        if (AssetsListBox.SelectedItems?.Contains(item) ?? false)
        {
            e.Handled = true;
            _suppressSelectionChange = true;
        }

        _assetDragArgs = e;
        _dragStartPosition = e.GetPosition(AssetsListBox);
    }

    private void OnAssetItemPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_assetDragArgs is null) return;

        var pos = e.GetPosition(AssetsListBox);
        var delta = pos - _dragStartPosition;
        if (Math.Abs(delta.X) < 8 && Math.Abs(delta.Y) < 8) return;

        var args = _assetDragArgs;
        ResetDragState();

        TaskService.RunDispatcher(async () => await StartDragAsync(args));
    }

    private void OnAssetItemPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var suppress = _suppressSelectionChange;
        var dragArgs = _assetDragArgs;
        ResetDragState();

        if (!suppress || dragArgs is null) return;
        if (e.Source is not Control { DataContext: AssetItem item }) return;

        var isMultiSelect = (e.KeyModifiers & KeyModifiers.Control) != 0
                         || (e.KeyModifiers & KeyModifiers.Shift) != 0;

        if (isMultiSelect)
        {
            if (AssetsListBox.SelectedItems?.Contains(item) == true)
                AssetsListBox.SelectedItems.Remove(item);
            else
                AssetsListBox.SelectedItems?.Add(item);
        }
        else
        {
            AssetsListBox.SelectedItems?.Clear();
            AssetsListBox.SelectedItems?.Add(item);
        }
    }

    private void ResetDragState()
    {
        _suppressSelectionChange = false;
        _assetDragArgs = null;
    }

    private async Task StartDragAsync(PointerEventArgs e)
    {
        var paths = ViewModel.AssetLoader.ActiveLoader!.SelectedAssetInfos
            .OfType<AssetInfo>()
            .Select(asset => asset.Asset.CreationData.Object.GetPathName())
            .ToArray();

        var dragDropInfoFile = await WriteDragDropInfoAsync(paths);

        TaskService.Run(ExportClient.DiscoverAsync);

        var storageFile = await TopLevel.GetTopLevel(this)!
            .StorageProvider
            .TryGetFileFromPathAsync(new Uri(dragDropInfoFile));

        if (storageFile is null) return;

        var data = new DataObject();
        data.Set(DataFormats.Files, new[] { storageFile });

        await DragDrop.DoDragDrop(e, data, DragDropEffects.Copy | DragDropEffects.Move);
    }

    private static async Task<string> WriteDragDropInfoAsync(string[] paths)
    {
        var filePath = Path.Combine(App.DataFolder.FullName, "info.fp_drag_drop");
        await File.WriteAllTextAsync(filePath, JsonConvert.SerializeObject(new { Paths = paths.ToList() }));
        return filePath;
    }
}