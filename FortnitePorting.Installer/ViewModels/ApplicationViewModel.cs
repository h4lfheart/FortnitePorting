using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;
using FortnitePorting.Framework.Services;
using FortnitePorting.Installer.Views;

namespace FortnitePorting.Installer.ViewModels;

public partial class ApplicationViewModel : ViewModelBase
{
    [ObservableProperty] private string versionString = $"v{Globals.VERSION}";

    [ObservableProperty] private UserControl? currentView;

    [ObservableProperty] private bool useFallbackBackground = Environment.OSVersion.Platform != PlatformID.Win32NT || (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Build < 22000);

    public ApplicationViewModel()
    {
        SetView<MainView>();
    }
    
    public void SetView<T>() where T : UserControl, new()
    {
        TaskService.RunDispatcher(() => CurrentView = new T());
    }
}