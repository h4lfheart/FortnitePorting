using System;
using System.IO;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace FortnitePorting.Application;

public class App : Avalonia.Application
{
    public static IClassicDesktopStyleApplicationLifetime Desktop;
    public static readonly DirectoryInfo AssetsFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets"));
    public static readonly DirectoryInfo LogsFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));
    public static readonly DirectoryInfo DataFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".data"));
    public static readonly DirectoryInfo CacheFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cache"));

    public override void OnFrameworkInitializationCompleted()
    {
        #if DEBUG
          ConsoleExtensions.AttachConsole(-1);
        #endif
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Desktop = desktop;
            desktop.Startup += OnStartup;
            desktop.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static void HandleException(Exception exception)
    {
        Log.Error("{0}", exception);
        Dispatcher.UIThread.Invoke(() =>
        {
            var errorWindow = new MessageWindow("An unhandled exception has occurred", $"{exception.GetType().FullName}: {exception.Message}", ApplicationService.ApplicationLifetime!.MainWindow);
            errorWindow.Show();
        });
        
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        AppSettings.Save();
        Log.CloseAndFlush();
    }

    private void OnStartup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
    {
        CUE4Parse.Globals.WarnMissingImportPackage = false;
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
            .WriteTo.File(Path.Combine(LogsFolder.FullName, $"FortnitePorting-{DateTime.UtcNow:yyyy-MM-dd-hh-mm-ss}.log"))
            .CreateLogger();

        AppSettings.Load();
        
        AssetsFolder.Create();
        DataFolder.Create();
        LogsFolder.Create();
        
        if (AppSettings.Current.UseDiscordRPC)
        {
            DiscordService.Initialize();
        }

        
        AppVM = new ApplicationViewModel();
        Desktop.MainWindow = new AppWindow();
    }
}