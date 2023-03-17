using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CSCore;
using CSCore.Codecs;
using CSCore.Codecs.OGG;
using CSCore.Codecs.WAV;
using CSCore.CoreAudioAPI;
using CSCore.MediaFoundation;
using CSCore.SoundOut;
using CSCore.Streams;
using CUE4Parse_Conversion.Sounds;
using CUE4Parse.GameTypes.FN.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Sound.Node;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.Utils;
using FortnitePorting.AppUtils;
using FortnitePorting.Bundles;
using FortnitePorting.Exports;
using FortnitePorting.Exports.Types;
using FortnitePorting.OpenGL;
using FortnitePorting.OpenGL.Renderable;
using FortnitePorting.Services;
using FortnitePorting.Services.Export;
using FortnitePorting.Views;
using FortnitePorting.Views.Controls;
using FortnitePorting.Views.Extensions;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using StyleSelector = FortnitePorting.Views.Controls.StyleSelector;

namespace FortnitePorting.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(StyleImage))] [NotifyPropertyChangedFor(nameof(StyleVisibility))]
    private List<IExportableAsset> extendedAssets = new();

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(StyleImage))] [NotifyPropertyChangedFor(nameof(StyleVisibility))]
    private IExportableAsset? currentAsset;

    public ImageSource? StyleImage => currentAsset?.FullSource;
    public Visibility StyleVisibility => currentAsset is null ? Visibility.Collapsed : Visibility.Visible;


    [ObservableProperty] private ObservableCollection<AssetSelectorItem> outfits = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> backBlings = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> harvestingTools = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> gliders = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> weapons = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> dances = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> props = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> vehicles = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> pets = new();
    [ObservableProperty] private SuppressibleObservableCollection<TreeItem> meshes = new();
    [ObservableProperty] private SuppressibleObservableCollection<AssetItem> assets = new();
    [ObservableProperty] private SuppressibleObservableCollection<AssetSelectorItem> musicPacks = new();

    [ObservableProperty] private ObservableCollection<StyleSelector> styles = new();

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(LoadingVisibility))]
    private bool isReady;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(LoadingVisibility))]
    private string tabModeText;

    [ObservableProperty] private EAssetType currentAssetType;

    public Visibility LoadingVisibility => IsReady ? Visibility.Collapsed : Visibility.Visible;

    [ObservableProperty] private ESortType sortType;

    public bool IsInitialized;

    public bool ShowConsole
    {
        get => AppSettings.Current.ShowConsole;
        set => AppSettings.Current.ShowConsole = value;
    }

    [ObservableProperty] private bool ascending;

    [ObservableProperty] private string searchFilter = string.Empty;
    [ObservableProperty] private ObservableCollection<Predicate<AssetSelectorItem>> filters = new();
    [ObservableProperty] private string filterLabel = "None";
    [ObservableProperty] private bool hiddenAssets;

    public Dictionary<string, Predicate<AssetSelectorItem>> FilterPredicates = new()
    {
        { "Favorite", x => AppSettings.Current.FavoriteIDs.Contains(x.ID, StringComparer.OrdinalIgnoreCase) },
        { "Battle Pass", x => x.GameplayTags.ContainsAny("BattlePass") },
        { "Item Shop", x => x.GameplayTags.ContainsAny("ItemShop") },
        { "Save The World", x => x.GameplayTags.ContainsAny("CampaignHero", "SaveTheWorld") },
        { "Battle Royale", x => !x.GameplayTags.ContainsAny("CampaignHero", "SaveTheWorld") },
        { "Unfinished Assets", x => x.HiddenAsset }
    };

    public MusicPackPlayer? CurrentMusicPlayer = null;

    [ObservableProperty] private EAnimGender animationGender;

    public async Task Initialize()
    {
        await Task.Run(async () =>
        {
            var loadTime = new Stopwatch();
            loadTime.Start();
            AppVM.CUE4ParseVM = new CUE4ParseViewModel(AppSettings.Current.ArchivePath, AppSettings.Current.InstallType);
            await AppVM.CUE4ParseVM.Initialize();
            loadTime.Stop();

            AppLog.Information($"Loaded FortniteGame Archive in {Math.Round(loadTime.Elapsed.TotalSeconds, 3)}s");
            IsReady = true;
            
            AppVM.AssetHandlerVM = new AssetHandlerViewModel();
            await AppVM.AssetHandlerVM.Initialize();
            IsInitialized = true;
            
        });
    }

    public FStructFallback[] GetSelectedStyles()
    {
        return CurrentAsset?.Type is EAssetType.Prop or EAssetType.Mesh ? Array.Empty<FStructFallback>() : Styles.Select(style => ((StyleSelectorItem)style.Options.Items[style.Options.SelectedIndex]).OptionData).ToArray();
    }

    [RelayCommand]
    public async Task Menu(string parameter)
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
            case "Settings_Startup":
                AppHelper.OpenWindow<StartupView>();
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
            case "Help_About":
                // TODO
                break;
            case "Update":
                CheckUpdate();
                break;
            case "SyncPlugin":
                AppHelper.OpenWindow<PluginUpdateView>();
                break;
            case "Tools_Heightmap":
                AppHelper.OpenWindow<HeightmapView>();
                break;
        }
    }

    public void CheckUpdate()
    {
        UpdateService.Start();
    }

    private static readonly string[] AllowedMeshTypes =
    {
        "Skeleton",
        "SkeletalMesh",
        "StaticMesh"
    };

    public async Task SetupMeshSelection(string path)
    {
        var meshObject = await AppVM.CUE4ParseVM.Provider.LoadObjectAsync(path);
        if (AllowedMeshTypes.Contains(meshObject.ExportType))
        {
            CurrentAsset = new MeshAssetItem(meshObject);
        }
    }
    
    public async Task SetupMeshSelection(AssetItem[] extendedItems)
    {
        ExtendedAssets.Clear();
        TabModeText = "SELECTED MESHES";
        var index = 0;
        foreach (var item in extendedItems)
        {
            var meshObject = await AppVM.CUE4ParseVM.Provider.LoadObjectAsync(item.PathWithoutExtension);
            if (AllowedMeshTypes.Contains(meshObject.ExportType))
            {
                var meshItem = new MeshAssetItem(meshObject);
                if (index == 0) CurrentAsset = meshItem;
                ExtendedAssets.Add(meshItem);
                index++;
            }
        }
        Styles.Clear();
        Styles.Add(new StyleSelector(ExtendedAssets));
      
    }

    public async Task<List<ExportDataBase>> CreateExportDatasAsync()
    {
        var exportAssets = new List<IExportableAsset>();
        if (ExtendedAssets.Count > 0)
        {
            exportAssets.AddRange(extendedAssets);
        }
        else if (CurrentAsset is not null)
        {
            exportAssets.Add(CurrentAsset);
        }

        var exportDatas = new List<ExportDataBase>();
        foreach (var asset in exportAssets)
        {
            await Task.Run(async () =>
            {
                var downloadedBundles = (await BundleDownloader.DownloadAsync(asset.Asset.Name)).ToList();
                if (downloadedBundles.Count > 0)
                {
                    downloadedBundles.ForEach(AppVM.CUE4ParseVM.Provider.RegisterFile);
                    await AppVM.CUE4ParseVM.Provider.MountAsync();
                }
            });

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

        var exportSettings = AppSettings.Current.BlenderExportSettings;
        exportSettings.AnimGender = AnimationGender;
        BlenderService.Client.Send(exportDatas, exportSettings);
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

        UnrealService.Client.Send(exportDatas, AppSettings.Current.UnrealExportSetttings);
    }

    [RelayCommand]
    public async Task OpenSettings()
    {
        AppHelper.OpenWindow<ImportSettingsView>();
    }

    [RelayCommand]
    public async Task Favorite()
    {
        if (CurrentAsset is AssetSelectorItem assetSelectorItem)
            assetSelectorItem?.ToggleFavorite();
    }

    [RelayCommand]
    public async Task ClearFilters()
    {
        Filters.Clear();
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
    public async Task PreviewMesh()
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

    [RelayCommand]
    public async Task PlaySound()
    {
        CurrentMusicPlayer?.Stop();
        var (data, format) = GetCurrentSoundData();
        CurrentMusicPlayer = new MusicPackPlayer(data, format);
        CurrentMusicPlayer.Play();
        
        DiscordService.UpdateMusicState(CurrentAsset?.DisplayName ?? string.Empty);
    }

    [RelayCommand]
    public async Task StopSound()
    {
        CurrentMusicPlayer?.Stop();
        DiscordService.ClearMusicState();
    }

    [RelayCommand]
    public async Task ExportSound()
    {
        var sound = GetCurrentSound();
        ExportHelpers.SaveSoundWave(sound.SoundWave, out _, out var path);
        AppHelper.Launch(Path.GetDirectoryName(path));
    }

    private Sound GetCurrentSound()
    {
        var musicCue = CurrentAsset?.Asset.GetOrDefault<USoundCue>("FrontEndLobbyMusic");
        var sound = ExportHelpers.HandleAudioTree(musicCue.FirstNode.Load<USoundNode>()).Last();
        return sound;
    }

    private (byte[] data, string format) GetCurrentSoundData()
    {
        var sound = GetCurrentSound();
        sound.SoundWave.Decode(true, out var format, out var data);
        return (data, format);
    }

    public class MusicPackPlayer : IDisposable
    {
        private ISoundOut? SoundOut;
        private IWaveSource? SoundSource;
        private static MMDeviceEnumerator? DeviceEnumerator;

        public MusicPackPlayer(byte[] data, string audioFormat)
        {
            if (!audioFormat.ToLower().Equals("ogg"))
            {
                AppVM.Warning("Unsupported Audio Format", $"The music pack could not be loaded because the format \"{audioFormat}\" is not supported.");
                Dispose();
                return;
            }


            DeviceEnumerator ??= new MMDeviceEnumerator();
            SoundSource = new LoopStream(new OggSource(new MemoryStream(data)).ToWaveSource());
            SoundOut = GetSoundOut();
            SoundOut.Initialize(SoundSource);
        }

        public void Play()
        {
            if (SoundOut is null) return;

            SoundOut.Volume = 0.8f;
            SoundOut.Play();
        }

        public void Stop()
        {
            if (SoundOut is null) return;

            SoundOut.Stop();
        }

        private static ISoundOut GetSoundOut()
        {
            if (WasapiOut.IsSupportedOnCurrentPlatform)
            {
                return new WasapiOut
                {
                    Device = GetDevice()
                };
            }

            return new DirectSoundOut();
        }

        public static MMDevice GetDevice()
        {
            return DeviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
        }

        public void Dispose()
        {
            Stop();
            SoundOut?.Dispose();
            SoundSource?.Dispose();
        }
    }
}