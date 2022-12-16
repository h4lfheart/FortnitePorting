using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Assets.Objects;
using FortnitePorting.AppUtils;
using FortnitePorting.Bundles;
using FortnitePorting.Exports;
using FortnitePorting.Exports.Types;
using FortnitePorting.Services;
using FortnitePorting.Views;
using FortnitePorting.Views.Controls;
using StyleSelector = FortnitePorting.Views.Controls.StyleSelector;

namespace FortnitePorting.ViewModels;

public partial class MainViewModel : ObservableObject
{

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StyleImage))]
    [NotifyPropertyChangedFor(nameof(StyleVisibility))]
    private AssetSelectorItem? currentAsset;

    public ImageSource? StyleImage => currentAsset?.FullSource;
    public Visibility StyleVisibility => currentAsset is null ? Visibility.Collapsed : Visibility.Visible;

    [ObservableProperty] private ObservableCollection<AssetSelectorItem> outfits = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> backBlings = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> harvestingTools = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> gliders = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> weapons = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> dances = new();
    
    [ObservableProperty] private ObservableCollection<StyleSelector> styles = new();

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(LoadingVisibility))]
    private bool isReady;

    public EAssetType CurrentAssetType;

    public Visibility LoadingVisibility => IsReady ? Visibility.Collapsed : Visibility.Visible;

    [ObservableProperty] private ESortType sortType;

    public bool IsInitialized;

    public async Task Initialize()
    {
        await Task.Run(async () =>
        {
            var loadTime = new Stopwatch();
            loadTime.Start();
            AppVM.CUE4ParseVM = new CUE4ParseViewModel(AppSettings.Current.ArchivePath, AppSettings.Current.InstallType);
            await AppVM.CUE4ParseVM.Initialize();
            loadTime.Stop();

            AppLog.Information($"Finished loading game files in {Math.Round(loadTime.Elapsed.TotalSeconds, 3)}s");
            IsReady = true;

            AppVM.AssetHandlerVM = new AssetHandlerViewModel();
            await AppVM.AssetHandlerVM.Initialize();
            IsInitialized = true;
        });
    }

    public FStructFallback[] GetSelectedStyles()
    {
        return Styles.Select(style => ((StyleSelectorItem) style.Options.Items[style.Options.SelectedIndex]).OptionData).ToArray();
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
            case "Settings_Startup":
                AppHelper.OpenWindow<StartupView>();
                break;
            case "Help_Update":
                // TODO
                break;
            case "Help_Discord":
                AppHelper.Launch(Globals.DISCORD_URL);
                break;
            case "Help_GitHub":
                AppHelper.Launch(Globals.GITHUB_URL);
                break;
            case "Help_About":
                // TODO
                break;
        }
    }

    [RelayCommand]
    public async Task ExportBlender()
    {
        if (CurrentAsset is null) return;
        if (!BlenderService.IsServerRunning())
        {
            AppVM.Warning("Failed to Establish Connection with FortnitePorting Server", "Please make sure you have installed the FortnitePortingServer.zip file and have an instance of Blender open.");
            return;
        }

        var downloadedBundles = (await BundleDownloader.DownloadAsync(CurrentAsset.Asset.Name)).ToList();
        if (downloadedBundles.Count > 0)
        {
            downloadedBundles.ForEach(AppVM.CUE4ParseVM.Provider.RegisterFile);
            await AppVM.CUE4ParseVM.Provider.MountAsync();
        }

        ExportDataBase exportData = CurrentAsset.Type switch
        {
            EAssetType.Dance => await DanceExportData.Create(CurrentAsset.Asset),
            _ => await MeshExportData.Create(CurrentAsset.Asset, CurrentAssetType, GetSelectedStyles())
        };
        
        BlenderService.Send(exportData, AppSettings.Current.BlenderExportSettings);
    }

    [RelayCommand]
    public async Task ExportUnreal()
    {

    }

    [RelayCommand]
    public async Task OpenSettings()
    {
        AppHelper.OpenWindow<ImportSettingsView>();
    }
    
    [RelayCommand]
    public async Task Favorite()
    {
        CurrentAsset?.ToggleFavorite();
    }

}