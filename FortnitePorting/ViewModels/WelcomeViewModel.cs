using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Framework;
using FortnitePorting.Views;
using Newtonsoft.Json;
using Serilog;
using InstallationProfile = FortnitePorting.Models.Installation.InstallationProfile;

namespace FortnitePorting.ViewModels;

public partial class WelcomeViewModel : ViewModelBase
{
    [ObservableProperty] private InstallationProfile _profile = new()
    {
        ProfileName = "Default",
        ArchiveDirectory = null
    };
    
    public override async Task Initialize()
    {
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
        Log.Information("Found Fortnite Installation at {ArchivePath}", Profile.ArchiveDirectory);
    }

    [RelayCommand]
    public async Task FinishSetup()
    {
        AppSettings.Installation.Profiles.Add(Profile);
        AppSettings.Installation.FinishedSetup = true;
        
        AppSettings.Application.NextKofiAskDate = DateTime.Today.AddDays(7);
        
        Navigation.App.Open<HomeView>();
        
        AppSettings.Save();
    }
}

file class LauncherInstalled
{
    public List<LauncherInstalledInfo> InstallationList;
}

file class LauncherInstalledInfo
{
    public string InstallLocation;
    public string AppVersion;
    public string AppName;
}