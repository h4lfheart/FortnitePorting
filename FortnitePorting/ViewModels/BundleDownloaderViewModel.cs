using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.AppUtils;

namespace FortnitePorting.ViewModels;

public class BundleDownloaderViewModel : ObservableObject
{
    public bool BundleDownloaderEnabled
    {
        get => AppSettings.Current.BundleDownloaderEnabled;
        set  {
            AppSettings.Current.BundleDownloaderEnabled = value;
            OnPropertyChanged();
        }
    }
}