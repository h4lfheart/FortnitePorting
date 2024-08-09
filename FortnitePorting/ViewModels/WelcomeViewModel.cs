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
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ArchiveDirectoryEnabled))]
    [NotifyPropertyChangedFor(nameof(UnrealVersionEnabled))]
    [NotifyPropertyChangedFor(nameof(EncryptionKeyEnabled))]
    [NotifyPropertyChangedFor(nameof(MappingsFileEnabled))]
    [NotifyPropertyChangedFor(nameof(TextureStreamingEnabled))]
    [NotifyPropertyChangedFor(nameof(CanFinishSetup))]
    [NotifyPropertyChangedFor(nameof(IsCustom))]
    private EFortniteVersion _fortniteVersion = EFortniteVersion.LatestInstalled;
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(CanFinishSetup))]
    [NotifyDataErrorInfo]
    [ArchiveDirectory]
    private string _archiveDirectory;
    
    
    [ObservableProperty] private EGame _unrealVersion = EGame.GAME_UE5_LATEST;
    [ObservableProperty] private FileEncryptionKey _mainKey;
    
    [ObservableProperty] private int _selectedExtraKeyIndex;
    [ObservableProperty] private ObservableCollection<FileEncryptionKey> _extraKeys = [];
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(MappingsFileEnabled))]
    private bool _useMappingsFile;
    
    [ObservableProperty] private string _mappingsFile;
    
    [ObservableProperty] private ELanguage _gameLanguage = ELanguage.English;
    [ObservableProperty] private bool _useTextureStreaming = true;

    public bool IsCustom => FortniteVersion is EFortniteVersion.Custom;

    public bool ArchiveDirectoryEnabled => FortniteVersion is not EFortniteVersion.LatestOnDemand;
    public bool UnrealVersionEnabled => IsCustom;
    public bool EncryptionKeyEnabled => IsCustom;
    public bool MappingsFileEnabled => IsCustom;
    public bool TextureStreamingEnabled => FortniteVersion is EFortniteVersion.LatestInstalled;
    
    // TODO convert all to validators?
    public bool CanFinishSetup => !HasErrors && FortniteVersion switch
    {
        EFortniteVersion.LatestOnDemand => true,
        _ => !string.IsNullOrWhiteSpace(ArchiveDirectory) && Directory.Exists(ArchiveDirectory)
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

        ArchiveDirectory = fortniteInfo.InstallLocation + "\\FortniteGame\\Content\\Paks\\";
        Log.Information("Found Fortnite Installation at {ArchivePath}", ArchiveDirectory);
    }

    [RelayCommand]
    public async Task FinishSetup()
    {
        var profile = new InstallationProfile
        {
            ProfileName = "Default",
            FortniteVersion = FortniteVersion,
            ArchiveDirectory = ArchiveDirectory,
            UnrealVersion = UnrealVersion,
            MainKey = MainKey,
            UseMappingsFile = UseMappingsFile,
            MappingsFile = MappingsFile,
            GameLanguage = GameLanguage,
            UseTextureStreaming = UseTextureStreaming
        };
        
        AppSettings.Current.Installation.Profiles.Add(profile);
        AppSettings.Current.Installation.FinishedWelcomeScreen = true;
        
        AppWM.SetupTabsAreVisible = false;
        AppWM.Navigate<HomeView>();
        
        AppSettings.Save();
    }
    
    [RelayCommand]
    public async Task BrowseArchivePath()
    {
        if (await BrowseFolderDialog() is { } path)
        {
            ArchiveDirectory = path;
        }
    }
    
    [RelayCommand]
    public async Task BrowseMappingsFile()
    {
        if (await BrowseFileDialog(fileTypes: Globals.MappingsFileType) is { } path)
        {
            MappingsFile = path;
        }
    }
    
    public async Task AddEncryptionKey()
    {
        ExtraKeys.Add(FileEncryptionKey.Empty);
    }
    
    public async Task RemoveEncryptionKey()
    {
        var selectedIndexToRemove = SelectedExtraKeyIndex;
        ExtraKeys.RemoveAt(selectedIndexToRemove);
        SelectedExtraKeyIndex = selectedIndexToRemove == 0 ? 0 : selectedIndexToRemove - 1;
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