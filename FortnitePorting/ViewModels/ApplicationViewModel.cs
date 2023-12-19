using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Application;
using FortnitePorting.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Framework.Application;
using FortnitePorting.Framework.Controls;
using FortnitePorting.Services;
using FortnitePorting.Framework.Services;
using FortnitePorting.Views;

namespace FortnitePorting.ViewModels;

public partial class ApplicationViewModel : ViewModelBase
{
    [ObservableProperty] private string versionString = $"v{Globals.VersionString}";

    [ObservableProperty] private UserControl? currentView;

    [ObservableProperty] private bool useFallbackBackground = Environment.OSVersion.Platform != PlatformID.Win32NT || (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Build < 22000);

    public ApplicationViewModel()
    {
        switch (AppSettings.Current.LoadingType)
        {
            case ELoadingType.Live:
            case ELoadingType.Local when AppSettings.Current.HasValidLocalData:
            case ELoadingType.Custom when AppSettings.Current.HasValidCustomData:
                SetView<MainView>();
                break;
            default:
                SetView<WelcomeView>();
                break;
        }
    }

    public void SetView<T>() where T : UserControl, new()
    {
        CurrentView = new T();
    }

    public void RestartWithMessage(string caption, string message)
    {
        MessageWindow.Show(caption, message, MainWindow, [new MessageWindowButton("Restart", _ => Restart())]);
    }

    public void Restart()
    {
        Launch(AppDomain.CurrentDomain.FriendlyName, false);
        Shutdown();
    }

    public void Shutdown()
    {
        AppBase.Application.Shutdown();
    }
}