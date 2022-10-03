using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.AppUtils;
using FortnitePorting.Views;
using FortnitePorting.Views.Controls;

namespace FortnitePorting.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> outfits = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> backBlings = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> harvestingTools = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> gliders = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> weapons = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> dances = new();
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LoadingVisibility))]
    private bool isReady;

    public Visibility LoadingVisibility => IsReady ? Visibility.Collapsed : Visibility.Visible;

    public async Task Initialize()
    {
        var loadTime = new Stopwatch();
        loadTime.Start();
        AppVM.CUE4ParseVM = new CUE4ParseViewModel(AppSettings.Current.ArchivePath);
        await AppVM.CUE4ParseVM.Initialize();
        loadTime.Stop();

        AppLog.Information($"Finished loading game files in {Math.Round(loadTime.Elapsed.TotalSeconds, 3)}s");
        IsReady = true;
        
        AppVM.AssetHandlerVM = new AssetHandlerViewModel();
        await AppVM.AssetHandlerVM.Initialize();

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
            case "Open_Exports":
                AppHelper.Launch(App.ExportsFolder.FullName);
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
            case "Tools_BundleDownloader":
                AppHelper.OpenWindow<BundleDownloaderView>();
                break;
            case "Tools_Update":
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
}