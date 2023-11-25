using System;
using System.IO;
using System.Reflection;
using Avalonia.Controls.ApplicationLifetimes;
using CUE4Parse.UE4.Assets;
using FortnitePorting.Export;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using AssetLoader = Avalonia.Platform.AssetLoader;

namespace FortnitePorting.Application;

public class App : Avalonia.Application
{
    public static readonly DirectoryInfo AssetsFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets"));
    public static readonly DirectoryInfo LogsFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));
    public static readonly DirectoryInfo DataFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".data"));
    public static readonly DirectoryInfo ChunkCacheFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".chunkcache"));
    public static readonly DirectoryInfo AudioCacheFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".audiocache"));

    public override void OnFrameworkInitializationCompleted()
    {
#if _WINDOWS
        ConsoleExtensions.AllocConsole();
#endif

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            ApplicationService.Application = desktop;
            desktop.Startup += OnStartup;
            desktop.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static void HandleException(Exception exception)
    {
        Log.Error("{0}", exception);
        TaskService.RunDispatcher(() => { MessageWindow.Show("An unhandled exception has occurred", $"{exception.GetType().FullName}: {exception.Message}", ApplicationService.Application.MainWindow); });
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        AppSettings.Save();
        Log.CloseAndFlush();
        RadioVM.Stop();
    }

    private void OnStartup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
    {
        Console.Title = $"Fortnite Porting Console v{Globals.VERSION}";
        CUE4Parse.Globals.WarnMissingImportPackage = false;
        ObjectTypeRegistry.RegisterEngine(typeof(URegisterThisUObject).Assembly);
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
            .WriteTo.File(Path.Combine(LogsFolder.FullName, $"FortnitePorting-{DateTime.UtcNow:yyyy-MM-dd-hh-mm-ss}.log"))
            .CreateLogger();

        AppSettings.Load();

        AssetsFolder.Create();
        DataFolder.Create();
        LogsFolder.Create();
        ChunkCacheFolder.Create();
        AudioCacheFolder.Create();

        if (AppSettings.Current.UseDiscordRPC) DiscordService.Initialize();

        DependencyService.EnsureDependencies();

        AppVM = new ApplicationViewModel();
        ApplicationService.Application.MainWindow = new AppWindow();
    }
}