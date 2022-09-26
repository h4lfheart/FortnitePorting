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
        }
    }
    
    public ERichPresenceAccess DiscordRPC
    {
        get => AppSettings.Current.DiscordRPC;
        set
        {
            AppSettings.Current.DiscordRPC = value;
            OnPropertyChanged();
        }
    }
    
}