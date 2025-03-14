using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DK.WshRuntime;
using FortnitePorting.Launcher.Models.Installation;
using FortnitePorting.Launcher.Models.Repository;
using FortnitePorting.Launcher.Services;
using FortnitePorting.Shared.Framework;

namespace FortnitePorting.Launcher.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty] private ProfilesViewModel _profiles = new();
    
    [ObservableProperty] private string _installationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortnitePortingLauncher", "Installations");
    [ObservableProperty] private string _downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortnitePortingLauncher", "Downloads");
    [ObservableProperty] private bool _launchOnStartup = false;
    [ObservableProperty] private bool _minimizeToTray = false;
    
    [ObservableProperty] private ObservableCollection<RepositoryUrlContainer> _repositories = [];
    [ObservableProperty] private ObservableCollection<InstallationVersion> _downloadedVersions = [];
    
    [ObservableProperty] private bool _finishedSetup = false;
    
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

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        switch (e.PropertyName)
        {
            case nameof(LaunchOnStartup):
            {
                var appPath = Environment.ProcessPath;
                var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                var shortcutPath = Path.Combine(startupFolder, "FortnitePorting.Launcher.lnk");
                if (LaunchOnStartup)
                {
                    if (!File.Exists(shortcutPath))
                        WshInterop.CreateShortcut(shortcutPath, string.Empty, appPath, "--startup", string.Empty);
                }
                else
                {
                    File.Delete(shortcutPath);
                }
                
                break;
            }
        }
    }
}