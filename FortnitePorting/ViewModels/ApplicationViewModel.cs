using System;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Runtime;
using FortnitePorting.Views;

namespace FortnitePorting.ViewModels;

public class ApplicationViewModel : ObservableObject
{
    public MainViewModel MainVM;
    public StartupViewModel StartupVM;
    public SettingsViewModel SettingsVM;
    public BundleDownloaderViewModel BundleDownloaderVM;

    public void Restart()
    {
        AppHelper.Launch(AppDomain.CurrentDomain.FriendlyName, shellExecute: false);
        Application.Current.Shutdown();
    }
    
    public void Quit()
    {
        Application.Current.Shutdown();
    }
}