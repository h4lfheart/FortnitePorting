using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using FortnitePorting.Framework.Controls;
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
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            Application = desktopLifetime;
            Application.Startup += Startup;
            Application.Exit += Exit;
        }
        
        base.OnFrameworkInitializationCompleted();
    }

    public static void HandleException(Exception exception)
    {
        Log.Error("{0}", exception);
        TaskService.RunDispatcher(() => MessageWindow.Show("An unhandled exception has occurred", $"{exception.GetType().FullName}: {exception.Message}"));
    }
    
    public static async Task<string?> BrowseFolderDialog(string startLocation = "")
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false, SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(startLocation)});
        var folder = folders.ToArray().FirstOrDefault();

        return folder?.Path.AbsolutePath.Replace("%20", " ");
    }

    public static async Task<string?> BrowseFileDialog(params FilePickerFileType[] fileTypes)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false, FileTypeFilter = fileTypes });
        var file = files.ToArray().FirstOrDefault();

        return file?.Path.AbsolutePath.Replace("%20", " ");
    }

    public static async Task<string?> SaveFileDialog(FilePickerSaveOptions saveOptions = default)
    {
        var file = await StorageProvider.SaveFilePickerAsync(saveOptions);
        return file?.Path.AbsolutePath.Replace("%20", " ");
    }
    
    public static void Launch(string location, bool shellExecute = true)
    {
        Process.Start(new ProcessStartInfo { FileName = location, UseShellExecute = shellExecute });
    }

    public static void Restart()
    {
        Launch(AppDomain.CurrentDomain.FriendlyName, false);
        Shutdown();
    }

    public static void Shutdown()
    {
        Application.Shutdown();
    }
}