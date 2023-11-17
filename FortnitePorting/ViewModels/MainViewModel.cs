using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSCore;
using CSCore.Codecs.OGG;
using CSCore.Codecs.WAV;
using CUE4Parse_Conversion.Sounds;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using FortnitePorting.AppUtils;
using FortnitePorting.Exports;
using FortnitePorting.Exports.Types;
using FortnitePorting.OpenGL;
using FortnitePorting.Services;
using FortnitePorting.Services.Export;
using FortnitePorting.Views;
using FortnitePorting.Views.Controls;
using FortnitePorting.Views.Extensions;
using MercuryCommons.Utilities.Extensions;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace FortnitePorting.ViewModels;

public partial class MainViewModel : ObservableObject
{
    // Asset Stuff
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentAssetImage))]
    [NotifyPropertyChangedFor(nameof(AssetPreviewVisibility))]
    [NotifyPropertyChangedFor(nameof(DefaultControlVisibility))]
    [NotifyPropertyChangedFor(nameof(MeshControlVisibility))]
    [NotifyPropertyChangedFor(nameof(MusicControlVisibility))]
    [NotifyPropertyChangedFor(nameof(DanceControlVisibility))]
    [NotifyPropertyChangedFor(nameof(LoadingScreenControlVisibility))]
    [NotifyPropertyChangedFor(nameof(SprayControlVisibility))]
    [NotifyPropertyChangedFor(nameof(BannerControlVisibility))]
    private IExportableAsset? currentAsset;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentAssetImage))]
    [NotifyPropertyChangedFor(nameof(AssetPreviewVisibility))]
    private List<IExportableAsset> extendedAssets = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValidFilterer))]
    [NotifyPropertyChangedFor(nameof(AssetTabVisibility))]
    [NotifyPropertyChangedFor(nameof(MeshTabVisibility))]
    private EAssetType currentAssetType = EAssetType.Outfit;

    [ObservableProperty]
    private EAnimGender animationGender = EAnimGender.Male;

    public bool IsValidFilterer => CurrentAssetType is not EAssetType.Mesh;
    public ImageSource? CurrentAssetImage => CurrentAsset?.FullSource;
    public Visibility AssetPreviewVisibility => CurrentAsset is null ? Visibility.Hidden : Visibility.Visible;

    public Visibility AssetTabVisibility => CurrentAssetType is EAssetType.Mesh ? Visibility.Collapsed : Visibility.Visible;
    public Visibility MeshTabVisibility => CurrentAssetType is EAssetType.Mesh ? Visibility.Visible : Visibility.Collapsed;

    public Visibility DefaultControlVisibility => CurrentAsset?.Type is not (EAssetType.Mesh or EAssetType.Dance or EAssetType.Music or EAssetType.LoadingScreen or EAssetType.Spray or EAssetType.Banner) ? Visibility.Visible : Visibility.Collapsed;
    public Visibility MeshControlVisibility => CurrentAsset?.Type is EAssetType.Mesh ? Visibility.Visible : Visibility.Collapsed;
    public Visibility MusicControlVisibility => CurrentAsset?.Type is EAssetType.Music ? Visibility.Visible : Visibility.Collapsed;
    public Visibility DanceControlVisibility => CurrentAsset?.Type is EAssetType.Dance ? Visibility.Visible : Visibility.Collapsed;
    public Visibility LoadingScreenControlVisibility => CurrentAsset?.Type is EAssetType.LoadingScreen ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SprayControlVisibility => CurrentAsset?.Type is EAssetType.Spray ? Visibility.Visible : Visibility.Collapsed;
    public Visibility BannerControlVisibility => CurrentAsset?.Type is EAssetType.Banner ? Visibility.Visible : Visibility.Collapsed;

    // Assets
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> outfits = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> backBlings = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> harvestingTools = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> gliders = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> items = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> dances = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> props = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> galleries = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> vehicles = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> pets = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> musicPacks = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> toys = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> wildlife = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> traps = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> loadingScreens = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> sprays = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> banners = new();

    [ObservableProperty] private SuppressibleObservableCollection<TreeItem> meshes = new();
    [ObservableProperty] private SuppressibleObservableCollection<AssetItem> assets = new();

    [ObservableProperty] private ObservableCollection<StyleSelector> styles = new();
    [ObservableProperty] private ObservableCollection<StyleSelector> meshPreviews = new();

    [ObservableProperty] private bool isPaused;
    [ObservableProperty] private string optionTabText;

    // Sort
    [ObservableProperty] private ESortType sortType;
    [ObservableProperty] private string searchFilter = string.Empty;
    [ObservableProperty] private bool ascending;

    // Filters
    [ObservableProperty] private Dictionary<string, Predicate<AssetSelectorItem>> filters = new();
    [ObservableProperty] private string filterLabel = "None";

    private static readonly Dictionary<string, Predicate<AssetSelectorItem>> FilterPredicates = new()
    {
        { "Favorite", x => AppSettings.Current.FavoriteIDs.Contains(x.ID, StringComparer.OrdinalIgnoreCase) },
        { "Battle Pass", x => x.GameplayTags.ContainsAny("BattlePass") },
        { "Item Shop", x => x.GameplayTags.ContainsAny("ItemShop") },
        { "Save The World", x => x.GameplayTags.ContainsAny("CampaignHero", "SaveTheWorld") || x.Asset.GetPathName().Contains("SaveTheWorld", StringComparison.OrdinalIgnoreCase) },
        { "Battle Royale", x => !x.GameplayTags.ContainsAny("CampaignHero", "SaveTheWorld") },
        { "Hidden Assets", x => x.HiddenAsset }
    };

    // Redirectors
    public bool ShowConsole
    {
        get => AppSettings.Current.ShowConsole;
        set => AppSettings.Current.ShowConsole = value;
    }

    private static readonly string[] AllowedMeshTypes =
    {
        "Skeleton",
        "SkeletalMesh",
        "StaticMesh"
    };

    public async Task SetupMeshSelection(string path)
    {
        ExtendedAssets.Clear();
        MeshPreviews.Clear();
        OptionTabText = "SELECTED MESHES";

        var meshObject = await AppVM.CUE4ParseVM.Provider.LoadObjectAsync(path);
        CurrentAsset = AllowedMeshTypes.Contains(meshObject.ExportType) ? new MeshAssetItem(meshObject) : null;
    }

    public async Task SetupMeshSelection(AssetItem[] extendedItems)
    {
        ExtendedAssets.Clear();
        MeshPreviews.Clear();
        OptionTabText = "SELECTED MESHES";

        var index = 0;
        var validMeshSelected = false;
        foreach (var item in extendedItems)
        {
            var meshObject = await AppVM.CUE4ParseVM.Provider.TryLoadObjectAsync(item.PathWithoutExtension);
            meshObject ??= await Task.Run(() => { return AppVM.CUE4ParseVM.Provider.LoadAllObjects(item.PathWithoutExtension).FirstOrDefault(x => AllowedMeshTypes.Contains(x.ExportType)); });
            if (meshObject is null) continue;

            if (AllowedMeshTypes.Contains(meshObject.ExportType))
            {
                var meshItem = new MeshAssetItem(meshObject);
                if (index == 0) CurrentAsset = meshItem;
                ExtendedAssets.Add(meshItem);
                index++;
                validMeshSelected = true;
            }
        }

        MeshPreviews.Add(new StyleSelector(ExtendedAssets));

        if (!validMeshSelected) CurrentAsset = null;
    }

    public FStructFallback[] GetSelectedStyles()
    {
        return CurrentAsset?.Type is EAssetType.Prop or EAssetType.Mesh ? Array.Empty<FStructFallback>() : Styles.Select(style => ((StyleSelectorItem) style.Options.Items[style.Options.SelectedIndex]).OptionData).ToArray();
    }

    private async Task<List<ExportDataBase>> CreateExportDatasAsync()
    {
        var exportAssets = new List<IExportableAsset>();
        if (ExtendedAssets.Count > 0)
        {
            exportAssets.AddRange(ExtendedAssets);
        }
        else if (CurrentAsset is not null)
        {
            exportAssets.Add(CurrentAsset);
        }

        var exportDatas = new List<ExportDataBase>();
        foreach (var asset in exportAssets)
        {
            ExportDataBase? exportData = asset.Type switch
            {
                EAssetType.Dance => await DanceExportData.Create(asset.Asset),
                _ => await MeshExportData.Create(asset.Asset, asset.Type, GetSelectedStyles())
            };

            if (exportData is null) continue;

            exportDatas.Add(exportData);
        }

        return exportDatas;
    }

    [RelayCommand]
    public async Task ExportBlender()
    {
        if (!BlenderService.Client.PingServer())
        {
            AppVM.Warning("Failed to Establish Connection with FortnitePorting Server", "Please make sure you have installed the latest FortnitePorting Blender plugin and have an instance of Blender open.");
            return;
        }

        var exportDatas = await CreateExportDatasAsync();
        if (exportDatas.Count == 0) return;

        var settings = AppSettings.Current.BlenderExportSettings;
        settings.AnimGender = AnimationGender;
        BlenderService.Client.Send(exportDatas, settings);
    }

    [RelayCommand]
    public async Task ExportUnreal()
    {
        if (!UnrealService.Client.PingServer())
        {
            AppVM.Warning("Failed to Establish Connection with FortnitePorting Server", "Please make sure you have installed the latest FortnitePorting Unreal Plugin and have an instance of Unreal Engine open.");
            return;
        }

        var exportDatas = await CreateExportDatasAsync();
        if (exportDatas.Count == 0) return;

        UnrealService.Client.Send(exportDatas, AppSettings.Current.UnrealExportSettings);
    }

    [RelayCommand]
    public void Menu(string parameter)
    {
        switch (parameter)
        {
            case "Open_Assets":
                AppHelper.Launch(App.AssetsFolder.FullName);
                break;
            case "Open_Data":
                AppHelper.Launch(App.DataFolder.FullName);
                break;
            case "Open_Logs":
                AppHelper.Launch(App.LogsFolder.FullName);
                break;
            case "File_Restart":
                AppVM.Restart();
                break;
            case "File_Quit":
                AppVM.Quit();
                break;
            case "Settings_Options":
                AppHelper.OpenWindow<SettingsView>();
                break;
            case "Settings_ImportOptions":
                AppHelper.OpenWindow<ImportSettingsView>();
                break;
            case "Help_Discord":
                AppHelper.Launch(Globals.DISCORD_URL);
                break;
            case "Help_GitHub":
                AppHelper.Launch(Globals.GITHUB_URL);
                break;
            case "Help_Donate":
                AppHelper.Launch(Globals.KOFI_URL);
                break;
            case "Sync_Blender":
                // TODO REDO BLENDER UPDATER
                AppHelper.OpenWindow<PluginUpdateView>();
                break;
            case "Sync_Unreal":
                AppHelper.OpenWindow<UnrealPluginView>();
                break;
            case "Update":
                UpdateService.Start();
                break;
            case "Tool_Heightmap":
                AppHelper.OpenWindow<HeightmapView>();
                break;
        }
    }

    [RelayCommand]
    public void Favorite()
    {
        if (CurrentAsset is AssetSelectorItem assetSelectorItem)
        {
            assetSelectorItem.ToggleFavorite();
        }
    }

    public void ModifyFilters(string tag, bool enable)
    {
        if (!FilterPredicates.ContainsKey(tag)) return;

        if (enable)
        {
            Filters.AddUnique(tag, FilterPredicates[tag]);
        }
        else
        {
            Filters.Remove(tag);
        }

        if (Filters.Count > 0)
        {
            FilterLabel = Filters.Keys.CommaJoin(includeAnd: false);
        }
        else
        {
            FilterLabel = "None";
        }
    }

    [RelayCommand]
    private void PreviewMesh()
    {
        AppVM.MeshViewer ??= new Viewer(GameWindowSettings.Default, new NativeWindowSettings
        {
            Size = new Vector2i(960, 540),
            NumberOfSamples = 8,
            WindowBorder = WindowBorder.Resizable,
            Profile = ContextProfile.Core,
            APIVersion = new Version(4, 6),
            Title = "Model Viewer",
            StartVisible = true,
            Flags = ContextFlags.ForwardCompatible
        });

        if (CurrentAssetType != EAssetType.Mesh)
        {
            AppVM.AssetHandlerVM.Handlers[CurrentAssetType].PauseState.Pause();
            AppVM.MeshViewer.Closing += _ => AppVM.AssetHandlerVM.Handlers[CurrentAssetType].PauseState.Unpause();
        }

        AppVM.MeshViewer.LoadMeshAssets(ExtendedAssets);
        AppVM.MeshViewer.Run();
    }

    [RelayCommand]
    private void AddToQueue()
    {
        AppHelper.OpenWindow<MusicView>();
        AppVM.MusicVM.Add(new MusicQueueItem(currentAsset));
    }

    [RelayCommand]
    private void ExportMusic()
    {
        var properSoundWave = MusicQueueItem.GetProperSoundWave(CurrentAsset.Asset);
        properSoundWave.Decode(true, out var format, out var data);
        if (data is null)
        {
            properSoundWave.Decode(false, out format, out data);
        }
        if (data is null) return;

        var exportFormat = format;
        var exportData = data;
        switch (format.ToLower())
        {
            case "adpcm":
                exportData = MusicQueueItem.ConvertedDataVGMStream(data).ReadToEnd();
                exportFormat = "wav";
                break;
            case "binka":
                exportData = MusicQueueItem.ConvertedDataBinkadec(data).ReadToEnd();
                exportFormat = "wav";
                break;
        }


        var path = ExportHelpers.GetExportPath(properSoundWave, exportFormat);
        File.WriteAllBytes(path, exportData);
        AppHelper.Launch(Path.GetDirectoryName(path));
    }
    
    
    [RelayCommand]
    private void PreviewLoadingScreen()
    {
        var texture = CurrentAsset.Asset.Get<UTexture2D>("BackgroundImage");
        AppHelper.OpenWindow<ImageViewerView>();
        AppVM.ImageVM.Initialize(texture, CurrentAsset.DisplayName);
    }
    
    
    [RelayCommand]
    private void ExportLoadingScreen()
    {
        var texture = CurrentAsset.Asset.Get<UTexture2D>("BackgroundImage");
        ExportHelpers.Save(texture, out var path);
        AppHelper.Launch(Path.GetDirectoryName(path));
    }
    
    [RelayCommand]
    private void ExportSpray()
    {
        var texture = CurrentAsset.Asset.Get<UTexture2D>("DecalTexture");
        ExportHelpers.Save(texture, out var path);
        AppHelper.Launch(Path.GetDirectoryName(path));
    }
    
    [RelayCommand]
    private void ExportBanner()
    {
        var texture = CurrentAsset.Asset.Get<UTexture2D>("LargePreviewImage");
        ExportHelpers.Save(texture, out var path);
        AppHelper.Launch(Path.GetDirectoryName(path));
    }
}