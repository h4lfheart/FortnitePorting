using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Launcher.Models.Installation;
using FortnitePorting.Launcher.Models.Repository;
using FortnitePorting.Shared.Framework;

namespace FortnitePorting.Launcher.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty] private ProfilesViewModel _profiles = new();
    
    [ObservableProperty] private string _installationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortnitePortingLauncher", "Installations");
    [ObservableProperty] private string _downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortnitePortingLauncher", "Downloads");
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
}