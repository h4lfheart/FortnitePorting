using System;
using System.Windows;
using System.Windows.Media.Imaging;
using AdonisUI;
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

        if (AppVM.SettingsVM.DiscordRPC)
        {
            DiscordService.Initialize();
        }
        else
        {
            DiscordService.DeInitialize();
        }
        
        ResourceLocator.SetColorScheme(Application.Current.Resources, AppSettings.Current.LightMode ? ResourceLocator.LightColorScheme : ResourceLocator.DarkColorScheme);
        MainView.YesWeDogs.Icon = new BitmapImage(new Uri(AppSettings.Current.LightMode ? 
                "pack://application:,,,/FortnitePorting-Dark.ico" :
                "pack://application:,,,/FortnitePorting.ico", UriKind.RelativeOrAbsolute));
        Close();
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
}