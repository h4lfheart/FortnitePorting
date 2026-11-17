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
        var archiveDirectory = await GetEGLV2ArchiveDirectory() ?? await GetEGLV1ArchiveDirectory();
        if (archiveDirectory is null) return;

        Profile.ArchiveDirectory = archiveDirectory;
        OnPropertyChanged(nameof(Profile));
        Log.Information("Found Fortnite Installation at {ArchivePath}", Profile.ArchiveDirectory);
    }

    private static async Task<string?> GetEGLV2ArchiveDirectory()
    {
        var installationsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "com.epicgames.eglv2", "installations.jsonl");
        if (!File.Exists(installationsFile)) return null;

        var installations = new Dictionary<string, EGLV2Installation>(StringComparer.OrdinalIgnoreCase);
        await foreach (var line in File.ReadLinesAsync(installationsFile))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            EGLV2Installation? installation;
            try
            {
                installation = JsonConvert.DeserializeObject<EGLV2Installation>(line);
            }
            catch (JsonException)
            {
                continue;
            }

            if (installation?.InstallationId is null) continue;
                
            installations[installation.InstallationId] = installation;
        }

        return installations.Values
            .Where(installation => installation.Complete
                                   && string.Equals(installation.Namespace, "fn", StringComparison.OrdinalIgnoreCase)
                                   && string.Equals(installation.ArtifactId, "Fortnite", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(installation => installation.WrittenAt)
            .Select(installation => Path.Combine(installation.Dir ?? string.Empty, "FortniteGame", "Content", "Paks"))
            .FirstOrDefault(Directory.Exists);
    }

    private static async Task<string?> GetEGLV1ArchiveDirectory()
    {
        EGLV1InstallationList? launcherInstalled = null;
        foreach (var drive in DriveInfo.GetDrives())
        {
            var launcherInstalledPath = $@"{drive.Name}ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat";
            if (!File.Exists(launcherInstalledPath)) continue;

            launcherInstalled = JsonConvert.DeserializeObject<EGLV1InstallationList>(await File.ReadAllTextAsync(launcherInstalledPath));
        }

        var fortniteInfo = launcherInstalled?.InstallationList.FirstOrDefault(x => x.AppName.Equals("Fortnite",  StringComparison.OrdinalIgnoreCase));
        if (fortniteInfo is null) return null;

        return fortniteInfo.InstallLocation + @"\FortniteGame\Content\Paks\";
    }
    
    [RelayCommand]
    public async Task Continue()
    {
        AppSettings.Installation.Profiles.Add(Profile);
        
        Navigation.Setup.Open<OnlineSetupView>();
    }
}


file class EGLV1InstallationList
{
    public List<EGLV1Installation> InstallationList = [];
}

file class EGLV1Installation
{
    public string InstallLocation;
    public string AppVersion;
    public string AppName;
}

file class EGLV2Installation
{
    [JsonProperty("installationId")] public string? InstallationId { get; set; }
    [JsonProperty("complete")] public bool Complete { get; set; }
    [JsonProperty("dir")] public string? Dir { get; set; }
    [JsonProperty("namespace")] public string? Namespace { get; set; }
    [JsonProperty("artifactId")] public string? ArtifactId { get; set; }
    [JsonProperty("version")] public string? Version { get; set; }
    [JsonProperty("writtenAt")] public DateTimeOffset? WrittenAt { get; set; }
}