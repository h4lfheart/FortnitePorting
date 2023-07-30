using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using FortnitePorting.AppUtils;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;
using FortnitePorting.Views.Extensions;

namespace FortnitePorting.Views;

public partial class SettingsView
{
    public SettingsView()
    {
        InitializeComponent();
        AppVM.SettingsVM = new SettingsViewModel();
        DataContext = AppVM.SettingsVM;
        
        AppVM.SettingsVM.AesKeys = new ObservableCollection<CustomAESKey>(AppSettings.Current.CustomAesKeys.ToArray());
    }

    private void OnClickOK(object sender, RoutedEventArgs e)
    {
        AppVM.SettingsVM.IsRestartRequired |= AppSettings.Current.CustomAesKeys != AppVM.SettingsVM.AesKeys.ToList();
        AppSettings.Current.CustomAesKeys = AppVM.SettingsVM.AesKeys.ToList();
        
        if (AppVM.SettingsVM.IsRestartRequired)
        {
            AppVM.RestartWithMessage("A restart is required.", "An option has been changed that requires a restart to take effect.");
        }

        if (AppVM.AssetHandlerVM is not null)
        {
            foreach (var handler in AppVM.AssetHandlerVM.Handlers.Values.Where(x => x.TargetCollection is not null))
            {
                foreach (var assetSelectorItem in handler.TargetCollection!)
                {
                    assetSelectorItem.SetSize(AppVM.SettingsVM.AssetSize);
                }
            }
        }

        if (AppVM.SettingsVM.ChangedUpdateChannel)
        {
            UpdateService.Start(automaticCheck: true);
        }

        if (AppVM.SettingsVM.DiscordRPC)
        {
            DiscordService.Initialize();
        }
        else
        {
            DiscordService.DeInitialize();
        }
    }

    private void OnClickInstallation(object sender, RoutedEventArgs e)
    {
        if (AppHelper.TrySelectFolder(out var path))
        {
            AppVM.SettingsVM.ArchivePath = path;
        }
    }

    private void OnClickExports(object sender, RoutedEventArgs e)
    {
        if (AppHelper.TrySelectFolder(out var path))
        {
            AppVM.SettingsVM.AssetsPath = path;
        }
    }

    private void OnClickMappings(object sender, RoutedEventArgs e)
    {
        if (AppHelper.TrySelectFile(out var path, filter: "Unreal Mappings|*.usmap"))
        {
            AppVM.SettingsVM.MappingsPath = path;
        }
    }

    private void OnClickAddKey(object sender, RoutedEventArgs e)
    {
        AppVM.SettingsVM.AesKeys.Add(CustomAESKey.ZERO);
    }
    
    private void OnClickRemoveKey(object sender, RoutedEventArgs e)
    {
        AppVM.SettingsVM.AesKeys.RemoveAt(AppVM.SettingsVM.AesKeys.Count-1);
    }
}