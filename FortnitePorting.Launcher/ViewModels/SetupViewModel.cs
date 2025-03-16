using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Launcher.Application;
using FortnitePorting.Launcher.Models.Repository;
using FortnitePorting.Launcher.Views;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Validators;

namespace FortnitePorting.Launcher.ViewModels;

public partial class SetupViewModel : ViewModelBase
{
    [ObservableProperty] 
    [DirectoryExists("Installation Path")]
    private string _installationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortnitePortingLauncher", "Installations");

    [ObservableProperty] 
    [DirectoryExists("Downloads Path")]
    private string _downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortnitePortingLauncher", "Downloads");
    
    [ObservableProperty] private bool _launchOnStartup = false;
    [ObservableProperty] private bool _minimizeToTray = false;

    [RelayCommand]
    public async Task Continue()
    {
        AppSettings.Current.InstallationPath = InstallationPath;
        AppSettings.Current.DownloadsPath = DownloadsPath;
        AppSettings.Current.Repositories = [new RepositoryUrlContainer(Globals.DEFAULT_FP_REPOSITORY)];
        AppSettings.Current.LaunchOnStartup = LaunchOnStartup;
        AppSettings.Current.MinimizeToTray = MinimizeToTray;
        AppSettings.Current.FinishedSetup = true;
        
        ViewModelRegistry.New<RepositoriesViewModel>(initialize: true);
        ViewModelRegistry.New<DownloadsViewModel>(initialize: true);
        
        AppWM.Navigate<ProfilesView>();

        var importDialog = new ContentDialog
        {
            Title = "Import Existing Installation",
            Content = "Would you like to import any existing installations?",
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            PrimaryButtonCommand = new RelayCommand(async () =>
            {
                await ProfilesVM.ImportInstallation();
            })
        };

        await importDialog.ShowAsync();
    }
    
    public async Task BrowseInstallationPath()
    {
        if (await BrowseFolderDialog() is { } path)
        {
            InstallationPath = path;
        }
    }
    
    public async Task BrowseDownloadsPath()
    {
        if (await BrowseFolderDialog() is { } path)
        {
            DownloadsPath = path;
        }
    }
}