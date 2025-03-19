using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Launcher.Application;
using FortnitePorting.Launcher.ViewModels;
using FortnitePorting.Launcher.Views;
using FortnitePorting.Launcher.WindowModels;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using AppWindow = FortnitePorting.Launcher.Windows.AppWindow;

namespace FortnitePorting.Launcher.Services;

public static class ApplicationService
{
    public static AppWindowModel AppWM => ViewModelRegistry.Get<AppWindowModel>()!;
    public static APIViewModel ApiVM => ViewModelRegistry.Get<APIViewModel>()!;
    public static DownloadsViewModel DownloadsVM => ViewModelRegistry.Get<DownloadsViewModel>()!;
    public static RepositoriesViewModel RepositoriesVM => ViewModelRegistry.Get<RepositoriesViewModel>()!;
    public static ProfilesViewModel ProfilesVM => ViewModelRegistry.Get<ProfilesViewModel>()!;
    
    public static IClassicDesktopStyleApplicationLifetime Application = null!;
    public static IStorageProvider StorageProvider => Application.MainWindow!.StorageProvider;
    public static IClipboard Clipboard => Application.MainWindow!.Clipboard!;
    
    public static readonly DirectoryInfo LogsFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));
    public static readonly DirectoryInfo LauncherDataFolder = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortnitePortingLauncher"));
    public static string LogFilePath;
    
    public static void Initialize()
    {
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

        LogFilePath = Path.Combine(LogsFolder.FullName, $"FortnitePorting-{DateTime.Now:yyyy-MM-dd-hh-mm-ss}.log");
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
            .WriteTo.File(LogFilePath)
            .CreateLogger();
        
        AppSettings.Load();
        
        LogsFolder.Create();
        LauncherDataFolder.Create();

        if (!Application.Args?.Contains("--startup") ?? true)
        {
            OpenAppWindow();
        }
        
        Application.Startup += OnStartup;
        Application.Exit += OnExit;
        
        Dispatcher.UIThread.UnhandledException += (sender, args) =>
        {
            args.Handled = true;
            HandleException(args.Exception);
        };
        
        TaskService.Exception += HandleException;
    }

    public static void OpenAppWindow()
    {
        if (Application.MainWindow is null)
        {
            Application.MainWindow = new AppWindow();
            Application.MainWindow.Loaded += OnWindowLoaded;
            Application.MainWindow.Show();
            return;
        }
        
        Application.MainWindow.WindowState = WindowState.Normal;
        Application.MainWindow.Show();
        Application.MainWindow.BringToTop();
    }

    private static void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        AppWM.FinishedSetup = AppSettings.Current.FinishedSetup;

        if (AppWM.FinishedSetup)
        {
            AppWM.Navigate<ProfilesView>();
        }
        else
        {
            AppWM.Navigate<SetupView>();
        }
    }
    
    private static void OnStartup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
    {
        ViewModelRegistry.NewOrExisting<AppWindowModel>();
        ViewModelRegistry.New<APIViewModel>();

        if (AppSettings.Current.FinishedSetup)
        {
            ViewModelRegistry.New<ProfilesViewModel>(initialize: true);
            ViewModelRegistry.New<RepositoriesViewModel>(initialize: true);
            ViewModelRegistry.New<DownloadsViewModel>(initialize: true);
        }
        
    }
    
    public static void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        AppSettings.Save();
    }
    
    public static void HandleException(Exception e)
    {
        var exceptionString = e.ToString();
        Log.Error(exceptionString);
                
        if (Application.MainWindow is null) return;
        
        TaskService.RunDispatcher(async () =>
        {
            var dialog = new ContentDialog
            {
                Title = "An unhandled exception has occurred",
                Content = exceptionString,
                CloseButtonText = "Continue"
            };
            await dialog.ShowAsync();
        });
    }
    
    public static void Launch(string location, bool shellExecute = true)
    {
        Process.Start(new ProcessStartInfo { FileName = location, UseShellExecute = shellExecute });
    }
    
    public static void LaunchSelected(string location)
    {
        var argument = "/select, \"" + location +"\"";
        Process.Start("explorer", argument);
    }
    
    public static async Task<string?> BrowseFolderDialog(string startLocation = "")
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false, SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(startLocation)});
        var folder = folders.ToArray().FirstOrDefault();

        return folder?.Path.AbsolutePath.Replace("%20", " ");
    }

    public static async Task<string?> BrowseFileDialog(string suggestedFileName = "", params FilePickerFileType[] fileTypes)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false, FileTypeFilter = fileTypes, SuggestedFileName = suggestedFileName});
        var file = files.ToArray().FirstOrDefault();

        return file?.Path.AbsolutePath.Replace("%20", " ");
    }

    public static async Task<string?> SaveFileDialog(string suggestedFileName = "", params FilePickerFileType[] fileTypes)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {FileTypeChoices = fileTypes, SuggestedFileName = suggestedFileName});
        return file?.Path.AbsolutePath.Replace("%20", " ");
    }
}