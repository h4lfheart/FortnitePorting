using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;
using FortnitePorting.Views;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using AppWindow = FortnitePorting.Windows.AppWindow;

namespace FortnitePorting.Services;

public static class ApplicationService
{
    public static AppViewModel AppVM => ViewModelRegistry.Get<AppViewModel>()!;
    public static WelcomeViewModel WelcomeVM => ViewModelRegistry.Get<WelcomeViewModel>()!;
    public static HomeViewModel HomeVM => ViewModelRegistry.Get<HomeViewModel>()!;
    public static CUE4ParseViewModel CUE4ParseVM => ViewModelRegistry.Get<CUE4ParseViewModel>()!;
    public static AssetsViewModel AssetsVM => ViewModelRegistry.Get<AssetsViewModel>()!;
    public static APIViewModel ApiVM => ViewModelRegistry.Get<APIViewModel>()!;
    
    public static IClassicDesktopStyleApplicationLifetime Application = null!;
    private static IStorageProvider StorageProvider => Application.MainWindow!.StorageProvider;
    public static IClipboard Clipboard => Application.MainWindow!.Clipboard!;
    
    public static readonly DirectoryInfo AssetsFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets"));
    public static readonly DirectoryInfo MapsFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Maps"));
    public static readonly DirectoryInfo LogsFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));
    public static readonly DirectoryInfo DataFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".data"));
    public static readonly DirectoryInfo CacheFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cache"));

    public static void Initialize()
    {
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
            .WriteTo.File(Path.Combine(LogsFolder.FullName, $"FortnitePorting-{DateTime.Now:yyyy-MM-dd-hh-mm-ss}.log"))
            .CreateLogger();
        
        AssetsFolder.Create();
        MapsFolder.Create();
        DataFolder.Create();
        LogsFolder.Create();
        CacheFolder.Create();
        
        AppSettings.Load();
        
        Application.MainWindow = new AppWindow();
        Application.Startup += OnStartup;
        Application.Exit += OnExit;
        Dispatcher.UIThread.UnhandledException += (sender, args) =>
        {
            args.Handled = true;

            var exceptionString = args.Exception.ToString();
            Log.Error(exceptionString);
                
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
        };
        
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            args.SetObserved();

            var exceptionString = args.Exception.ToString();
            Log.Error(exceptionString);
                
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
        };
    }

    public static void OnStartup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
    {
        DiscordService.Initialize();
        ViewModelRegistry.Register<APIViewModel>();
        DependencyService.EnsureDependencies();
        
        if (AppSettings.Current.FinishedWelcomeScreen)
        {
            AppVM.Navigate<HomeView>();
        }
        else
        {
            AppVM.Navigate<WelcomeView>();
        }
    }
    
    public static void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        AppSettings.Save();
        DiscordService.Deinitialize();
    }

    public static void DisplayDialog(string title, string content)
    {
        TaskService.RunDispatcher(async () =>
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "Continue"
            };
            
            await dialog.ShowAsync();
        });
    }
    
    public static void Launch(string location, bool shellExecute = true)
    {
        Process.Start(new ProcessStartInfo { FileName = location, UseShellExecute = shellExecute });
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
}