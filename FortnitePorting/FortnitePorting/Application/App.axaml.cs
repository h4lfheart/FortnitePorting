using System;
using System.IO;
using Avalonia.Controls.ApplicationLifetimes;
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
    
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Desktop = desktop;
        }
        
        Desktop.Startup += OnStartup;
        Desktop.Exit += OnExit;

        base.OnFrameworkInitializationCompleted();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        AppSettings.Save();
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