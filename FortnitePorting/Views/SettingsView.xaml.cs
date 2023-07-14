using System.Linq;
using System.Windows;
using FortnitePorting.AppUtils;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class SettingsView
{
    public SettingsView()
    {
        InitializeComponent();
        AppVM.SettingsVM = new SettingsViewModel();
        DataContext = AppVM.SettingsVM;
    }

    private void OnClickOK(object sender, RoutedEventArgs e)
    {
        if (AppVM.SettingsVM.IsRestartRequired)
        {
            AppVM.RestartWithMessage("A restart is required.", "An option has been changed that requires a restart to take effect.");
        }

        if (AppVM.AssetHandlerVM is not null)
        {
            foreach (var handler in AppVM.AssetHandlerVM.Handlers.Values.Where(x => x.TargetCollection is not null))
            {
                if (handler.AssetType is EAssetType.Gallery)
                {
                    foreach (var expander in AppVM.MainVM.Galleries)
                    {
                        foreach (var assetSelectorItem in expander.Props)
                        {
                            assetSelectorItem.SetSize(AppVM.SettingsVM.AssetSize);
                        }
                    }
                }
                else
                {
                    foreach (var assetSelectorItem in handler.TargetCollection!)
                    {
                        assetSelectorItem.SetSize(AppVM.SettingsVM.AssetSize);
                    }
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
        if (AppHelper.TrySelectFile(out var path))
        {
            AppVM.SettingsVM.MappingsPath = path;
        }
    }
}