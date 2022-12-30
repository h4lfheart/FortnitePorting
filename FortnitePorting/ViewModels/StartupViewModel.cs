using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Versions;
using FortnitePorting.AppUtils;
using Newtonsoft.Json;

namespace FortnitePorting.ViewModels;

public class StartupViewModel : ObservableObject
{
    public string ArchivePath
    {
        get => AppSettings.Current.ArchivePath;
        set
        {
            AppSettings.Current.ArchivePath = value;
            OnPropertyChanged();
        }
    }

    public ELanguage Language
    {
        get => AppSettings.Current.Language;
        set
        {
            AppSettings.Current.Language = value;
            OnPropertyChanged();
        }
    }

    public bool IsLocalInstall => InstallType == EInstallType.Local;
    public EInstallType InstallType
    {
        get => AppSettings.Current.InstallType;
        set
        {
            AppSettings.Current.InstallType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsLocalInstall));
        }
    }

    public void CheckForInstallation()
    {
        LauncherInstalled? launcherInstalled = null;
        foreach (var drive in DriveInfo.GetDrives())
        {
            var launcherInstalledPath = $"{drive.Name}ProgramData\\Epic\\UnrealEngineLauncher\\LauncherInstalled.dat";
            if (!File.Exists(launcherInstalledPath)) continue;

            launcherInstalled = JsonConvert.DeserializeObject<LauncherInstalled>(File.ReadAllText(launcherInstalledPath));
        }

        var fortniteInfo = launcherInstalled?.InstallationList.FirstOrDefault(x => x.AppName.Equals("Fortnite"));
        if (fortniteInfo is null) return;

        ArchivePath = fortniteInfo.InstallLocation + "\\FortniteGame\\Content\\Paks\\";
        Log.Information("Detected EGL Installation at {ArchivePath}", ArchivePath);
    }

    private class LauncherInstalled
    {
        public List<LauncherInstalledInfo> InstallationList;

        public class LauncherInstalledInfo
        {
            public string InstallLocation;
            public string NamespaceId; // useless
            public string ItemId; // useless
            public string ArtifactId; // useless
            public string AppVersion;
            public string AppName;
        }
    }
}