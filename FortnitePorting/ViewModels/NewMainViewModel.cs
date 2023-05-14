using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Assets.Objects;
using FortnitePorting.AppUtils;
using FortnitePorting.Exports.Types;
using FortnitePorting.OpenGL;
using FortnitePorting.Services;
using FortnitePorting.Services.Export;
using FortnitePorting.Views;
using FortnitePorting.Views.Controls;
using FortnitePorting.Views.Extensions;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace FortnitePorting.ViewModels;

public partial class NewMainViewModel : ObservableObject
{
    // Asset Stuff
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(CurrentAssetImage))]
    [NotifyPropertyChangedFor(nameof(AssetPreviewVisibility))]
    [NotifyPropertyChangedFor(nameof(DefaultControlVisibility))]
    [NotifyPropertyChangedFor(nameof(MeshControlVisibility))]
    [NotifyPropertyChangedFor(nameof(MusicControlVisibility))]
    private IExportableAsset? currentAsset;
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(CurrentAssetImage))] 
    [NotifyPropertyChangedFor(nameof(AssetPreviewVisibility))]
    private List<IExportableAsset> extendedAssets = new();
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(IsValidFilterer))]
    [NotifyPropertyChangedFor(nameof(AssetTabVisibility))]
    [NotifyPropertyChangedFor(nameof(GalleryTabVisibility))]
    [NotifyPropertyChangedFor(nameof(MeshTabVisibility))]
    private EAssetType currentAssetType = EAssetType.Outfit;
    
    public bool IsValidFilterer => CurrentAssetType is not (EAssetType.Gallery or EAssetType.Mesh);
    public ImageSource? CurrentAssetImage => CurrentAsset?.FullSource;
    public Visibility AssetPreviewVisibility => CurrentAsset is null ? Visibility.Hidden : Visibility.Visible;
    
    public Visibility AssetTabVisibility => CurrentAssetType is (EAssetType.Gallery or EAssetType.Mesh) ? Visibility.Collapsed : Visibility.Visible;
    public Visibility GalleryTabVisibility => CurrentAssetType is EAssetType.Gallery ? Visibility.Visible : Visibility.Collapsed;
    public Visibility MeshTabVisibility => CurrentAssetType is EAssetType.Mesh ? Visibility.Visible : Visibility.Collapsed;
    
    public Visibility DefaultControlVisibility => CurrentAsset?.Type is not (EAssetType.Mesh or EAssetType.Dance or EAssetType.Music) ? Visibility.Visible : Visibility.Collapsed;
    public Visibility MeshControlVisibility => CurrentAsset?.Type is EAssetType.Mesh ? Visibility.Visible : Visibility.Collapsed;
    public Visibility MusicControlVisibility => CurrentAsset?.Type is EAssetType.Music ? Visibility.Visible : Visibility.Collapsed;
    
    // Assets
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> outfits = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> backBlings = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> harvestingTools = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> gliders = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> weapons = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> dances = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> props = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> vehicles = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> pets = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> musicPacks = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> toys = new();
    
    [ObservableProperty] private ObservableCollection<PropExpander> galleries = new();
    
    [ObservableProperty] private SuppressibleObservableCollection<TreeItem> meshes = new();
    [ObservableProperty] private SuppressibleObservableCollection<AssetItem> assets = new();
    
    [ObservableProperty] private ObservableCollection<StyleSelector> styles = new();
    
    [ObservableProperty] private bool isPaused;
    [ObservableProperty] private string optionTabText;
    
    // Sort
    [ObservableProperty] private ESortType sortType;
    [ObservableProperty] private string searchFilter = string.Empty;
    [ObservableProperty] private bool ascending;
    
    // Filters
    [ObservableProperty] private ObservableCollection<Predicate<AssetSelectorItem>> filters = new();
    [ObservableProperty] private string filterLabel = "None";
    private static readonly Dictionary<string, Predicate<AssetSelectorItem>> FilterPredicates = new()
    {
        { "Favorite", x => AppSettings.Current.FavoriteIDs.Contains(x.ID, StringComparer.OrdinalIgnoreCase) },
        { "Battle Pass", x => x.GameplayTags.ContainsAny("BattlePass") },
        { "Item Shop", x => x.GameplayTags.ContainsAny("ItemShop") },
        { "Save The World", x => x.GameplayTags.ContainsAny("CampaignHero", "SaveTheWorld") },
        { "Battle Royale", x => !x.GameplayTags.ContainsAny("CampaignHero", "SaveTheWorld") },
        { "Unfinished Assets", x => x.HiddenAsset }
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
        Styles.Clear();
        OptionTabText = "SELECTED MESHES";
        
        var meshObject = await AppVM.CUE4ParseVM.Provider.LoadObjectAsync(path);
        CurrentAsset = AllowedMeshTypes.Contains(meshObject.ExportType) ? new MeshAssetItem(meshObject) : null;
    }
    
    public async Task SetupMeshSelection(AssetItem[] extendedItems)
    {
        ExtendedAssets.Clear();
        Styles.Clear();
        OptionTabText = "SELECTED MESHES";
        
        var index = 0;
        var validMeshSelected = false;
        foreach (var item in extendedItems)
        {
            var meshObject = await AppVM.CUE4ParseVM.Provider.LoadObjectAsync(item.PathWithoutExtension);
            if (AllowedMeshTypes.Contains(meshObject.ExportType))
            {
                var meshItem = new MeshAssetItem(meshObject);
                if (index == 0) CurrentAsset = meshItem;
                ExtendedAssets.Add(meshItem);
                index++;
                validMeshSelected = true;
            }
        }
        
        Styles.Add(new StyleSelector(ExtendedAssets));
      
        if (!validMeshSelected) CurrentAsset = null;
    }
    
    public FStructFallback[] GetSelectedStyles()
    {
        return CurrentAsset?.Type is EAssetType.Prop or EAssetType.Mesh or EAssetType.Gallery ? Array.Empty<FStructFallback>() : Styles.Select(style => ((StyleSelectorItem) style.Options.Items[style.Options.SelectedIndex]).OptionData).ToArray();
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
            AppVM.Warning("Failed to Establish Connection with FortnitePorting Server", "Please make sure you have installed the BlenderFortnitePortingServer.zip file and have an instance of Blender open.");
            return;
        }

        var exportDatas = await CreateExportDatasAsync();
        if (exportDatas.Count == 0) return;

        BlenderService.Client.Send(exportDatas, AppSettings.Current.BlenderExportSettings);
    }

    [RelayCommand]
    public async Task ExportUnreal()
    {
        if (!UnrealService.Client.PingServer())
        {
            AppVM.Warning("Failed to Establish Connection with FortnitePorting Server", "Please make sure you have installed the FortnitePorting Server Plugin and have an instance of Unreal Engine open.");
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
                // TODO
                break;
            case "Sync_Unreal":
                // TODO
                break;
            case "Update":
                UpdateService.Start();
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
        var predicate = FilterPredicates[tag];

        if (enable)
        {
            Filters.AddUnique(predicate);
        }
        else
        {
            Filters.Remove(predicate);
        }

        if (Filters.Count > 0)
        {
            FilterLabel = FilterPredicates.Where(x => Filters.Contains(x.Value)).Select(x => x.Key).CommaJoin(includeAnd: false);
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
            AppVM.AssetHandlerVM?.Handlers[CurrentAssetType].PauseState.Pause();
            AppVM.MeshViewer.Closing += _ => AppVM.AssetHandlerVM?.Handlers[CurrentAssetType].PauseState.Unpause();
        }
        
        AppVM.MeshViewer.LoadMeshAssets(ExtendedAssets);
        AppVM.MeshViewer.Run();
    }
}