using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using FortnitePorting.Framework.Services;
using Serilog;

namespace FortnitePorting.Framework.Application;

public abstract class AppBase : Avalonia.Application
{
    public static IClassicDesktopStyleApplicationLifetime Application;

    public static Window MainWindow
    {
        get => Application.MainWindow!;
        set => Application.MainWindow = value;
    }
    public static IStorageProvider StorageProvider => MainWindow.StorageProvider;
    public static IClipboard Clipboard => MainWindow.Clipboard!;

    public EventHandler<ControlledApplicationLifetimeStartupEventArgs> Startup;
    public EventHandler<ControlledApplicationLifetimeExitEventArgs> Exit;

    public AppBase(EventHandler<ControlledApplicationLifetimeStartupEventArgs> startup, EventHandler<ControlledApplicationLifetimeExitEventArgs> exit)
    {
        Startup = startup;
        Exit = exit;
    }
    
    public override void OnFrameworkInitializationCompleted()
    {
        Application = (IClassicDesktopStyleApplicationLifetime) ApplicationLifetime!;
        Application.Startup += Startup;
        Application.Exit += Exit;
        
        base.OnFrameworkInitializationCompleted();
    }

    public static void HandleException(Exception exception)
    {
        Log.Error("{0}", exception);
        //TaskService.RunDispatcher(() => { MessageWindow.Show("An unhandled exception has occurred", $"{exception.GetType().FullName}: {exception.Message}", ApplicationService.Application.MainWindow); });
    }
}