using System;
using System.IO;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CUE4Parse.UE4.Assets;
using FortnitePorting.Export;
using FortnitePorting.Framework;
using FortnitePorting.Framework.Application;
using FortnitePorting.Framework.Extensions;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace FortnitePorting.Application;

public class App : AppBase
{
    public static ApplicationViewModel AppVM = null!;
    public static MainViewModel MainVM => ViewModelRegistry.Get<MainViewModel>()!;
    public static HomeViewModel HomeVM => ViewModelRegistry.Get<HomeViewModel>()!;
    public static CUE4ParseViewModel CUE4ParseVM => ViewModelRegistry.Get<CUE4ParseViewModel>()!;
    public static AssetsViewModel AssetsVM => ViewModelRegistry.Get<AssetsViewModel>()!;
    public static FilesViewModel FilesVM => ViewModelRegistry.Get<FilesViewModel>()!;
    public static RadioViewModel RadioVM => ViewModelRegistry.Get<RadioViewModel>()!;
    public static EndpointViewModel EndpointsVM => ViewModelRegistry.Get<EndpointViewModel>()!;
    
    public static readonly DirectoryInfo AssetsFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets"));
    public static readonly DirectoryInfo LogsFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));
    public static readonly DirectoryInfo DataFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".data"));
    public static readonly DirectoryInfo ChunkCacheFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".chunkcache"));
    public static readonly DirectoryInfo AudioCacheFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".audiocache"));

    public App() : base(OnStartup, OnExit)
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected static void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        RadioVM.Stop();
        AppSettings.Save();
        Log.CloseAndFlush();
    }

    protected static void OnStartup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
    {
        ObjectTypeRegistry.RegisterEngine(typeof(URegisterThisUObject).Assembly);
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
            .WriteTo.File(Path.Combine(LogsFolder.FullName, $"FortnitePorting-{DateTime.Now:yyyy-MM-dd-hh-mm-ss}.log"))
            .CreateLogger();

        AppSettings.Load();
        if (AppSettings.Current.ShowConsole) ConsoleExtensions.AllocConsole();

        AssetsFolder.Create();
        DataFolder.Create();
        LogsFolder.Create();
        ChunkCacheFolder.Create();
        AudioCacheFolder.Create();

        if (AppSettings.Current.UseDiscordRPC) DiscordService.Initialize();

        DependencyService.EnsureDependencies();
        ViewModelRegistry.Register<EndpointViewModel>();

        AppVM = new ApplicationViewModel();
        MainWindow = new AppWindow();
    }
}