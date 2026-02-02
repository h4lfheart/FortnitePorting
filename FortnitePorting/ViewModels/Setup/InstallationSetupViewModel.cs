using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Framework;
using FortnitePorting.Models.Installation;
using FortnitePorting.Services;
using FortnitePorting.Views.Setup;
using Newtonsoft.Json;
using Serilog;

namespace FortnitePorting.ViewModels.Setup;

public partial class InstallationSetupViewModel : ViewModelBase
{
    [ObservableProperty] private InstallationProfile _profile = new()
    {
        ProfileName = "Default",
        ArchiveDirectory = string.Empty,
        IsSelected = true
    };
    
    public override async Task Initialize()
    {
        AppSettings.Installation.Profiles.Clear();
        
        await CheckForInstallation();
    }

    private async Task CheckForInstallation()
    {
        LauncherInstalled? launcherInstalled = null;
        foreach (var drive in DriveInfo.GetDrives())
        {
            var launcherInstalledPath = $@"{drive.Name}ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat";
            if (!File.Exists(launcherInstalledPath)) continue;

            launcherInstalled = JsonConvert.DeserializeObject<LauncherInstalled>(await File.ReadAllTextAsync(launcherInstalledPath));
        }

        var fortniteInfo = launcherInstalled?.InstallationList.FirstOrDefault(x => x.AppName.Equals("Fortnite",  StringComparison.OrdinalIgnoreCase));
        if (fortniteInfo is null) return;

        Profile.ArchiveDirectory = fortniteInfo.InstallLocation + @"\FortniteGame\Content\Paks\";
        OnPropertyChanged(nameof(Profile));
        Log.Information("Found Fortnite Installation at {ArchivePath}", Profile.ArchiveDirectory);
    }
    
    [RelayCommand]
    public async Task Continue()
    {
        AppSettings.Installation.Profiles.Add(Profile);
        
        Navigation.Setup.Open<OnlineSetupView>();
    }
}


file class LauncherInstalled
{
    public List<LauncherInstalledInfo> InstallationList = [];
}

file class LauncherInstalledInfo
{
    public string InstallLocation;
    public string AppVersion;
    public string AppName;
}