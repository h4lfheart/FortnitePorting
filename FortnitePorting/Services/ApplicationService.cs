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
using Microsoft.Win32;
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
    public static RadioViewModel RadioVM => ViewModelRegistry.Get<RadioViewModel>()!;
    public static APIViewModel ApiVM => ViewModelRegistry.Get<APIViewModel>()!;
    public static ChatViewModel ChatVM => ViewModelRegistry.Get<ChatViewModel>()!;
    public static FilesViewModel FilesVM => ViewModelRegistry.Get<FilesViewModel>()!;
    
    public static IClassicDesktopStyleApplicationLifetime Application = null!;
    private static IStorageProvider StorageProvider => Application.MainWindow!.StorageProvider;
    public static IClipboard Clipboard => Application.MainWindow!.Clipboard!;
    public static INotificationManager NotificationManager;
    
    public static readonly DirectoryInfo AssetsFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets"));
    public static readonly DirectoryInfo MapsFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Maps"));
    public static readonly DirectoryInfo LogsFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));
    public static readonly DirectoryInfo DataFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".data"));
    public static readonly DirectoryInfo CacheFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cache"));
    public static string LogFilePath;

    public static void Initialize()
    {
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

        LogFilePath = Path.Combine(LogsFolder.FullName, $"FortnitePorting-{DateTime.Now:yyyy-MM-dd-hh-mm-ss}.log");
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
            .WriteTo.FortnitePorting()
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
        Log.Information($"Install Mode: {AppSettings.Current.Installation.FortniteVersion.GetDescription()}");
        Log.Information($"Archive Path: {AppSettings.Current.Installation.ArchiveDirectory}");
        Log.Information($"Texture Streaming: {AppSettings.Current.Installation.UseTextureStreaming}");
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
                PrimaryButtonCommand = OpenLogCommand,
                SecondaryButtonText = "Open Console",
                SecondaryButtonCommand = GoToConsoleCommand,
                CloseButtonText = "Continue",
            };
            await dialog.ShowAsync();
        });
    }

    public static readonly RelayCommand GoToConsoleCommand = new(GoToConsole);
    public static void GoToConsole()
    {
        AppVM.Navigate<ConsoleView>();
    }
    
    public static readonly RelayCommand OpenLogCommand = new(OpenLog);
    public static void OpenLog()
    {
        LaunchSelected(LogFilePath);
    }

    public static void OnStartup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
    {
        ViewModelRegistry.New<APIViewModel>();
        DependencyService.EnsureDependencies();
        
        if (AppSettings.Current.Discord.UseIntegration)
        {
            TaskService.Run(async () =>
            {
                await AppSettings.Current.Discord.LoadIdentification();
                await ApiVM.FortnitePorting.PostStatsAsync();
            });
            
            GlobalChatService.Init();
        }
        
        if (AppSettings.Current.Discord.UseRichPresence)
        {
            DiscordService.Initialize();
        }
        
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
        foreach (var viewModel in ViewModelRegistry.All())
        {
            viewModel.OnApplicationExit();
        }
        
        AppSettings.Save();
        //DiscordService.Deinitialize();
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