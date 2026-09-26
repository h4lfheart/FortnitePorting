using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FortnitePorting.Controls.Navigation.Sidebar;
using FortnitePorting.Exporting;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.Assets.Asset;
using FortnitePorting.Models.Assets.Base;
using FortnitePorting.Models.Assets.Custom;
using FortnitePorting.Models.Assets.Filters;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Views;
using Material.Icons;

namespace FortnitePorting.ViewModels;

public partial class AssetsViewModel(
    AssetLoaderService assetLoader,
    ExportService exporter,
    SettingsService settings,
    InfoService info,
    NavigationService navigation,
    CUE4ParseService ueParse,
    AppService app,
    DiscordService discord,
    SupabaseService supabase) : ViewModelBase, IResettable
{
    [ObservableProperty] private AssetLoaderService _assetLoader = assetLoader;

    private readonly ExportService _exporter = exporter;
    private readonly SettingsService _settings = settings;
    private readonly InfoService _info = info;
    private readonly NavigationService _navigation = navigation;
    private readonly CUE4ParseService _ueParse = ueParse;
    private readonly AppService _app = app;
    private readonly DiscordService _discord = discord;
    private readonly SupabaseService _supaBase = supabase;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsTastyRigApplyVisible))]
    private EExportLocation _exportLocation = EExportLocation.Blender;

    [ObservableProperty] private bool _isExporting;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowNamesIcon))] private bool _showNames;
    [ObservableProperty] private ObservableCollection<ISidebarItem> _sidebarItems = [];

    private ExportDataMeta? _exportMeta;

    public bool IsTastyRigApplyVisible => ExportLocation is EExportLocation.Blender && _assetLoader.ActiveLoader?.Type is EExportType.Outfit;
    public MaterialIconKind ShowNamesIcon => ShowNames ? MaterialIconKind.TextLong : MaterialIconKind.TextShort;

    public void Reset()
    {
        SidebarItems.Clear();
        IsExporting = false;
        _exportMeta?.Dispose();
        _exportMeta = null;
        InvalidateInitialization();
    }

    public override async Task Initialize()
    {
        _assetLoader.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AssetLoaderService.ActiveLoader))
                OnPropertyChanged(nameof(IsTastyRigApplyVisible));
        };

        ShowNames = _settings.Application.ShowAssetNames;
        ExportLocation = _settings.Application.DefaultExportLocation;

        await TaskService.RunDispatcherAsync(() =>
        {
            foreach (var (index, category) in _assetLoader.Categories.Index())
            {
                var group = new SidebarItemGroup(category.Category.Description)
                {
                    IsExpanded = index == 0
                };

                foreach (var loader in category.Loaders)
                {
                    group.Items.Add(new SidebarItemButton(
                        text: loader.Type.Description,
                        iconBitmap: ImageExtensions.AvaresBitmap($"avares://FortnitePorting/Assets/FN/{loader.Type.ToString()}.png"),
                        tag: loader.Type
                    ));
                }

                SidebarItems.Add(group);

                if (index < _assetLoader.Categories.Count - 1)
                    SidebarItems.Add(new SidebarItemSeparator());
            }
        });

        _navigation.Assets.Open(_settings.Application.UseDefaultExportLoadType
            ? _settings.Application.DefaultExportLoadType
            : EExportType.Outfit);
    }

    public override async Task OnViewOpened()
    {
        if (_assetLoader.ActiveLoader is null) return;
        _navigation.Assets.Open(_assetLoader.ActiveLoader.Type);
    }

    public override async Task OnViewExited()
    {
        _settings.Application.ShowAssetNames = ShowNames;
    }

    [RelayCommand]
    public async Task SetExportLocation(EExportLocation location)
    {
        ExportLocation = location;
    }

    [RelayCommand]
    public async Task Export()
    {
        if (_assetLoader.ActiveLoader is null) return;

        _exportMeta = _settings.ExportSettings.CreateExportMeta(ExportLocation);
        IsExporting = true;

        try
        {
            var exportedProperly = await _exporter.Export(
                _assetLoader.ActiveLoader.SelectedAssetInfos,
                _exportMeta);
            if (exportedProperly && _supaBase.IsLoggedIn)
            {
                await _supaBase.PostExports([
                    .._assetLoader.ActiveLoader.SelectedAssetInfos
                        .OfType<AssetInfo>()
                        .Select(asset => asset.Asset.CreationData.Object.GetPathName()),
                    .._assetLoader.ActiveLoader.SelectedAssetInfos
                        .OfType<CustomAssetInfo>()
                        .Select(asset => $"Custom/{asset.Asset.Asset.Name}"),
                ]);
            }
        }
        finally
        {
            _exportMeta?.Dispose();
            _exportMeta = null;
            IsExporting = false;
        }
    }

    [RelayCommand]
    public void CancelExport()
    {
        _exportMeta?.Cancel();
    }

    [RelayCommand]
    public async Task Favorite()
    {
        foreach (var assetInfo in _assetLoader.ActiveLoader.SelectedAssetInfos)
            assetInfo.Asset.Favorite();
    }

    [RelayCommand]
    public async Task ExportTastyRig()
    {
        await _exporter.ExportTastyRig(_settings.ExportSettings.CreateExportMeta(ExportLocation));
    }

    private const string ExportIconsMessageId = "ExportAllIcons";

    [RelayCommand]
    public async Task ExportAllIcons()
    {
        if (_assetLoader.ActiveLoader is not { FinishedLoading: true } loader) return;
        if (await _app.BrowseFolderDialog() is not { } folderPath) return;

        var items = loader.Source.Items.OfType<AssetItem>().ToArray();
        var total = items.Length;
        var cts = new CancellationTokenSource();

        _info.Message("Exporting Icons", string.Empty, autoClose: false, id: ExportIconsMessageId,
            useButton: true, buttonTitle: "Cancel", buttonCommand: cts.Cancel,
            useProgress: true, progressCurrent: 0, progressTotal: total);

        await TaskService.RunAsync(async () =>
        {
            var sw = Stopwatch.StartNew();
            var saved = 0;

            for (var i = 0; i < items.Length; i++)
            {
                if (cts.Token.IsCancellationRequested) break;

                var item = items[i];
                var iconPath = item.CreationData.HighResIconPath ?? item.CreationData.LowResIconPath;
                if (iconPath is null) continue;

                var iconName = Path.GetFileNameWithoutExtension(iconPath);
                _info.UpdateMessage(ExportIconsMessageId, iconName);
                _info.UpdateMessageProgress(ExportIconsMessageId, i + 1, total);

                try
                {
                    var texture = await _ueParse.Provider.SafeLoadPackageObjectAsync<UTexture2D>(iconPath);
                    using var bitmap = texture?.Decode()?.ToSkBitmap()?.ToWriteableBitmap();
                    if (bitmap is null) continue;

                    bitmap.Save(Path.Combine(folderPath, $"{iconName}.png"));
                    saved++;
                }
                catch
                {
                    // skip items that fail to load/decode
                }
            }

            sw.Stop();
            _info.CloseMessage(ExportIconsMessageId);
            _info.Message("Icons Dumped", $"Exported {saved} assets in {sw.Elapsed.TotalSeconds:F3}s", closeTime: 6);
        });
    }

    [RelayCommand]
    public async Task OpenSettings()
    {
        _navigation.App.Open<ExportSettingsView>();
        _navigation.ExportSettings.Open(ExportLocation);
    }

    public void ChangeTab(EExportType assetType)
    {
        if (_assetLoader.ActiveLoader?.Type == assetType) return;

        _discord.Update(assetType);

        var loaders = _assetLoader.Categories.SelectMany(category => category.Loaders);
        foreach (var loader in loaders)
        {
            if (loader.Type == assetType)
                loader.Unpause();
            else
                loader.Pause();
        }

        TaskService.Run(async () => await _assetLoader.Load(assetType));
    }

    public void SyncSelectedAssets(IEnumerable<BaseAssetItem> selectedItems)
    {
        if (_assetLoader.ActiveLoader is null) return;

        _assetLoader.ActiveLoader.SelectedAssetInfos = [];
        foreach (var asset in selectedItems)
        {
            if (asset is AssetItem assetItem)
            {
                var stylePaths =
                    _assetLoader.ActiveLoader.StyleDictionary.GetValueOrDefault(asset.CreationData.DisplayName) ??
                    _assetLoader.ActiveLoader.StyleDictionary.GetValueOrDefault(asset.CreationData.ID);

                _assetLoader.ActiveLoader.SelectedAssetInfos.Add(
                    stylePaths is not null
                        ? new AssetInfo(assetItem, stylePaths.OrderBy(x => x.EndsWith(asset.CreationData.ID, StringComparison.OrdinalIgnoreCase) ? 0 : 1))
                        : new AssetInfo(assetItem));
            }
            else if (asset is CustomAssetItem customAsset)
            {
                _assetLoader.ActiveLoader.SelectedAssetInfos.Add(new CustomAssetInfo(customAsset));
            }
        }
    }

    public void UpdateFilter(FilterItem filterItem, bool isChecked)
    {
        _assetLoader.ActiveLoader?.UpdateFilters(filterItem, isChecked);
    }

    public int GetRandomIndex(int itemCount)
    {
        if (itemCount <= 0) return -1;
        return Random.Shared.Next(0, itemCount);
    }

    public string[] GetSelectedAssetPaths()
    {
        return _assetLoader.ActiveLoader?.SelectedAssetInfos
            .OfType<AssetInfo>()
            .Select(asset => asset.Asset.CreationData.Object.GetPathName())
            .ToArray() ?? [];
    }

    public void AdjustAssetScale(bool increase)
    {
        _settings.Application.AssetScale = float.Clamp(
            _settings.Application.AssetScale + (increase ? 0.25f : -0.25f),
            0.5f, 4.0f);
    }
}
