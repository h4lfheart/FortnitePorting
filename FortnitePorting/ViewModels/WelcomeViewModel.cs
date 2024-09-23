using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Versions;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Models.API;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.CUE4Parse;
using FortnitePorting.Models.Settings;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.Shared.Validators;
using FortnitePorting.Views;
using Newtonsoft.Json;
using RestSharp;
using Serilog;

namespace FortnitePorting.ViewModels;

public partial class WelcomeViewModel : ViewModelBase
{
    [ObservableProperty] private InstallationProfile _profile = new()
    {
        ProfileName = "Default"
    };
    
    public override async Task Initialize()
    {
        await CheckForInstallation();

        await AppSettings.Current.Online.PromptForAuthentication();
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
        
        AppSettings.Current.Installation.Profiles.Add(Profile);
        AppSettings.Current.Installation.FinishedWelcomeScreen = true;
        
        AppWM.SetupTabsAreVisible = false;
        AppWM.Navigate<HomeView>();
        
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