using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Versions;
using FortnitePorting.AppUtils;

namespace FortnitePorting.ViewModels;

public class SettingsViewModel : ObservableObject
{
    public bool IsRestartRequired = false;
    public bool ChangedUpdateChannel = false;

    public bool IsLiveInstall => InstallType == EInstallType.Live;
    public bool IsCustomInstall => InstallType == EInstallType.Custom;
    public bool CanChangePath => InstallType != EInstallType.Live;

    public EInstallType InstallType
    {
        get => AppSettings.Current.InstallType;
        set
        {
            AppSettings.Current.InstallType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsLiveInstall));
            OnPropertyChanged(nameof(IsCustomInstall));
            OnPropertyChanged(nameof(CanChangePath));
            IsRestartRequired = true;
        }
    }

    public string ArchivePath
    {
        get => AppSettings.Current.ArchivePath;
        set
        {
            AppSettings.Current.ArchivePath = value;
            OnPropertyChanged();
            IsRestartRequired = true;
        }
    }
    
    public EGame GameVersion
    {
        get => AppSettings.Current.GameVersion;
        set
        {
            AppSettings.Current.GameVersion = value;
            OnPropertyChanged();
            IsRestartRequired = true;
        }
    }
    
    public string MappingsPath
    {
        get => AppSettings.Current.MappingsPath;
        set
        {
            AppSettings.Current.MappingsPath = value;
            OnPropertyChanged();
            IsRestartRequired = true;
        }
    }
    
    public string AESKey
    {
        get => AppSettings.Current.AesKey;
        set
        {
            AppSettings.Current.AesKey = value;
            OnPropertyChanged();
            IsRestartRequired = true;
        }
    }

    public ELanguage Language
    {
        get => AppSettings.Current.Language;
        set
        {
            AppSettings.Current.Language = value;
            OnPropertyChanged();
            IsRestartRequired = true;
        }
    }

    public string AssetsPath
    {
        get => AppSettings.Current.AssetsPath;
        set
        {
            AppSettings.Current.AssetsPath = value;
            OnPropertyChanged();
        }
    }

    public bool DiscordRPC
    {
        get => AppSettings.Current.DiscordRichPresence;
        set
        {
            AppSettings.Current.DiscordRichPresence = value;
            OnPropertyChanged();
        }
    }

    public EUpdateMode UpdateMode
    {
        get => AppSettings.Current.UpdateMode;
        set
        {
            AppSettings.Current.UpdateMode = value;
            OnPropertyChanged();
            ChangedUpdateChannel = true;
            OnPropertyChanged(nameof(ChangedUpdateChannel));
        }
    }

    public float AssetSize
    {
        get => AppSettings.Current.AssetSize;
        set
        {
            AppSettings.Current.AssetSize = value;
            OnPropertyChanged();
        }
    }
}