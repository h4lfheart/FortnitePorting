using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Versions;
using FortnitePorting.AppUtils;

namespace FortnitePorting.ViewModels;

public class SettingsViewModel : ObservableObject
{
    public bool IsRestartRequired = false;

    public bool IsLocalInstall => InstallType == EInstallType.Local;
    public EInstallType InstallType
    {
        get => AppSettings.Current.InstallType;
        set
        {
            AppSettings.Current.InstallType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsLocalInstall));
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

    public bool BundleDownloaderEnabled
    {
        get => AppSettings.Current.BundleDownloaderEnabled;
        set
        {
            AppSettings.Current.BundleDownloaderEnabled = value;
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
        }
    }

    public bool LightMode
    {
        get => AppSettings.Current.LightMode;
        set
        {
            AppSettings.Current.LightMode = value;
            OnPropertyChanged();
            IsRestartRequired = true;
            OnPropertyChanged(nameof(IsRestartRequired));
        }
    }
}