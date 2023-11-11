using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Versions;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Views;
using Newtonsoft.Json;
using Serilog;
using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace FortnitePorting.ViewModels;

public partial class WelcomeViewModel : ViewModelBase
{
    public bool CanContinue => CurrentLoadingType switch
    {
        ELoadingType.Local => !string.IsNullOrWhiteSpace(LocalArchivePath) && Directory.Exists(LocalArchivePath),
        ELoadingType.Live => true,
        ELoadingType.Custom => !(string.IsNullOrWhiteSpace(LocalArchivePath) || string.IsNullOrWhiteSpace(CustomEncryptionKey)) && Directory.Exists(CustomArchivePath) && CustomEncryptionKey.TryParseAesKey(out _),
    };
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanContinue))] private ELoadingType currentLoadingType;  
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanContinue))] private string localArchivePath = null;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanContinue))] private string customArchivePath = null;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanContinue))] private string customMappingsPath = null;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanContinue))] private string customEncryptionKey = Globals.ZERO_CHAR;
    [ObservableProperty] private EGame customUnrealVersion = EGame.GAME_UE5_4;

    private static readonly FilePickerFileType MappingsFileType = new("Unreal Mappings")
    {
        Patterns = new[] { "*.usmap" }
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
            var launcherInstalledPath = $"{drive.Name}ProgramData\\Epic\\UnrealEngineLauncher\\LauncherInstalled.dat";
            if (!File.Exists(launcherInstalledPath)) continue;

            launcherInstalled = JsonConvert.DeserializeObject<LauncherInstalled>(await File.ReadAllTextAsync(launcherInstalledPath));
        }

        var fortniteInfo = launcherInstalled?.InstallationList.FirstOrDefault(x => x.AppName.Equals("Fortnite"));
        if (fortniteInfo is null) return;

        LocalArchivePath = fortniteInfo.InstallLocation + "\\FortniteGame\\Content\\Paks\\";
        Log.Information("Found Fortnite Installation at {ArchivePath}", LocalArchivePath);
    }
    
    [RelayCommand]
    private async Task BrowseLocalArchivePath()
    {
        if (await AppVM.BrowseFolderDialog() is {} path)
        {
            LocalArchivePath = path;
        }
    }
    
    [RelayCommand]
    private async Task BrowseCustomArchivePath()
    {
        if (await AppVM.BrowseFolderDialog() is {} path)
        {
            CustomArchivePath = path;
        }
    }

    [RelayCommand]
    private async Task BrowseMappingsFile()
    {
        if (await AppVM.BrowseFileDialog(MappingsFileType) is {} path)
        {
            CustomMappingsPath = path;
        }
    }
    
    [RelayCommand]
    private void Continue()
    {
        AppSettings.Current.LoadingType = CurrentLoadingType;
        AppSettings.Current.LocalArchivePath = LocalArchivePath;
        AppSettings.Current.CustomArchivePath = CustomArchivePath;
        AppSettings.Current.CustomEncryptionKey = CustomEncryptionKey;
        AppSettings.Current.CustomMappingsPath = CustomMappingsPath;
        
        AppVM.SetView<MainView>();
    }
}

public class LauncherInstalled
{
    [J] public List<LauncherInstalledInfo> InstallationList;
}

public class LauncherInstalledInfo
{
    [J] public string InstallLocation;
    [J] public string AppVersion;
    [J] public string AppName;
}