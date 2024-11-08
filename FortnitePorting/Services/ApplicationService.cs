using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopNotifications;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;
using FortnitePorting.Views;
using FortnitePorting.WindowModels;
using Microsoft.Win32;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using AppWindow = FortnitePorting.Windows.AppWindow;
using AppWindowModel = FortnitePorting.WindowModels.AppWindowModel;

namespace FortnitePorting.Services;

public static class ApplicationService
{
    public static AppWindowModel AppWM => ViewModelRegistry.Get<AppWindowModel>()!;
    public static SoundPreviewWindowModel SoundPreviewWM => ViewModelRegistry.Get<SoundPreviewWindowModel>()!;
    public static WelcomeViewModel WelcomeVM => ViewModelRegistry.Get<WelcomeViewModel>()!;
    public static HomeViewModel HomeVM => ViewModelRegistry.Get<HomeViewModel>()!;
    public static CUE4ParseViewModel CUE4ParseVM => ViewModelRegistry.Get<CUE4ParseViewModel>()!;
    public static AssetsViewModel AssetsVM => ViewModelRegistry.Get<AssetsViewModel>()!;
    public static RadioViewModel RadioVM => ViewModelRegistry.Get<RadioViewModel>()!;
    public static APIViewModel ApiVM => ViewModelRegistry.Get<APIViewModel>()!;
    public static ChatViewModel ChatVM => ViewModelRegistry.Get<ChatViewModel>()!;
    public static FilesViewModel FilesVM => ViewModelRegistry.Get<FilesViewModel>()!;
    public static HelpViewModel HelpVM => ViewModelRegistry.Get<HelpViewModel>()!;
    public static ConsoleViewModel ConsoleVM => ViewModelRegistry.Get<ConsoleViewModel>()!;
    public static LeaderboardViewModel LeaderboardVM => ViewModelRegistry.Get<LeaderboardViewModel>()!;
    public static VotingViewModel VotingVM => ViewModelRegistry.Get<VotingViewModel>()!;
    public static TimeWasterViewModel TimeWasterVM => ViewModelRegistry.Get<TimeWasterViewModel>()!;
    public static MapViewModel MapVM => ViewModelRegistry.Get<MapViewModel>()!;
    public static CanvasViewModel CanvasVM => ViewModelRegistry.Get<CanvasViewModel>()!;
    
    public static IClassicDesktopStyleApplicationLifetime Application = null!;
    private static IStorageProvider StorageProvider => Application.MainWindow!.StorageProvider;
    public static IClipboard Clipboard => Application.MainWindow!.Clipboard!;
    public static INotificationManager NotificationManager;
    
    public static readonly DirectoryInfo AssetsFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets"));
    public static readonly DirectoryInfo MapsFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Maps"));
    public static readonly DirectoryInfo LogsFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));
    public static readonly DirectoryInfo PluginsFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins"));
    public static readonly DirectoryInfo DataFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".data"));
    public static readonly DirectoryInfo CacheFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cache"));
    public static string LogFilePath;

    public static void Initialize()
    {
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

        ViewModelRegistry.New<ConsoleViewModel>();
        LogFilePath = Path.Combine(LogsFolder.FullName, $"FortnitePorting-{DateTime.Now:yyyy-MM-dd-hh-mm-ss}.log");
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
            .WriteTo.Sink(ConsoleVM)
            .WriteTo.File(LogFilePath)
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
            HandleException(args.Exception);
        };
        
        TaskService.Exception += HandleException;
        
        Log.Information($"Fortnite Porting {Globals.VersionString}");
        Log.Information($".NET Version: {RuntimeInformation.FrameworkDescription}");
        
        if (AppSettings.Current.Installation.CurrentProfile is not null)
        {
            Log.Information($"Install Mode: {AppSettings.Current.Installation.CurrentProfile.FortniteVersion.GetDescription()}");
            Log.Information($"Archive Path: {AppSettings.Current.Installation.CurrentProfile.ArchiveDirectory}");
            Log.Information($"Texture Streaming: {AppSettings.Current.Installation.CurrentProfile.UseTextureStreaming}");
        }
    }
    
    public static void HandleException(Exception e)
    {
        var exceptionString = e.ToString();
        Log.Error(exceptionString);
                
        TaskService.RunDispatcher(async () =>
        {
            var dialog = new ContentDialog
            {
                Title = "An unhandled exception has occurred",
                Content = exceptionString,
                
                PrimaryButtonText = "Open Log",
                PrimaryButtonCommand = new RelayCommand(() => LaunchSelected(LogFilePath)),
                SecondaryButtonText = "Open Console",
                SecondaryButtonCommand = new RelayCommand(() => AppWM.Navigate<ConsoleView>()),
                CloseButtonText = "Continue",
            };
            await dialog.ShowAsync();
        });
    }

    public static void OnStartup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
    {
        ViewModelRegistry.New<APIViewModel>();
        ViewModelRegistry.New<ChatViewModel>();
        ViewModelRegistry.New<CanvasViewModel>();
        ViewModelRegistry.New<VotingViewModel>(initialize: true);
        DependencyService.EnsureDependencies();
        
        TimeWasterViewModel.LoadResources();

        TaskService.Run(async () => await AppWM.Initialize());
        
        if (AppSettings.Current.Online.UseIntegration)
        {
            TaskService.Run(async () =>
            {
                await AppSettings.Current.Online.LoadIdentification();
                await ApiVM.FortnitePorting.PostStatsAsync();
                OnlineService.Init();
            });
        }
        
        if (AppSettings.Current.Online.UseRichPresence)
        {
            DiscordService.Initialize();
        }

        if (AppSettings.Current.Plugin.Blender.AutomaticallySync && DependencyService.Finished)
        {
            TaskService.Run(async () => await AppSettings.Current.Plugin.Blender.SyncInstallations(verbose: false));
        }
        
        if (AppSettings.Current.Installation.FinishedWelcomeScreen)
        {
            AppWM.Navigate<HomeView>();
        }
        else
        {
            AppWM.Navigate<WelcomeView>();
        }
    }
    
    public static void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        foreach (var viewModel in ViewModelRegistry.All())
        {
            viewModel.OnApplicationExit();
        }
        
        AppSettings.Save();
        DiscordService.Deinitialize();
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