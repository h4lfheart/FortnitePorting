using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using AdonisUI.Controls;
using CUE4Parse.UE4.Assets;
using FortnitePorting.AppUtils;
using FortnitePorting.Exports;
using FortnitePorting.Services;
using Serilog.Sinks.SystemConsole.Themes;
using Console = System.Console;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;
using MessageBoxResult = AdonisUI.Controls.MessageBoxResult;

namespace FortnitePorting;

public partial class App
{
    
    public static DirectoryInfo AssetsFolder => new(AppSettings.Current.AssetsPath);
    public static readonly DirectoryInfo DataFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".data"));
    public static readonly DirectoryInfo MapFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Terrain"));

    public static readonly DirectoryInfo LogsFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));

    public static readonly DirectoryInfo CacheFolder = new(Path.Combine(DataFolder.FullName, "ManifestCache"));
    public static readonly DirectoryInfo VGMStreamFolder = new(Path.Combine(DataFolder.FullName, "VGMStream"));

    public static readonly Random RandomGenerator = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        WindowsUtils.AllocConsole();
        WindowsUtils.InitExitHandler(_ =>
        {
            ExitHandler();
            return false;
        });
        Console.Title = $"Fortnite Porting Console - v{Globals.VERSION}";
        CUE4Parse.Globals.WarnMissingImportPackage = false;

        ObjectTypeRegistry.RegisterEngine(typeof(UFortnitePortingCustom).Assembly);
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

        Log.Logger = new LoggerConfiguration().WriteTo.Console(theme: AnsiConsoleTheme.Literate).WriteTo.File(Path.Combine(LogsFolder.FullName, $"FortnitePorting-{DateTime.UtcNow:yyyy-MM-dd-hh-mm-ss}.log")).CreateLogger();

        AppSettings.DirectoryPath.Create();
        AppSettings.Load();

        WindowsUtils.ToggleConsole(AppSettings.Current.ShowConsole);

        AssetsFolder.Create();
        DataFolder.Create();
        LogsFolder.Create();
        MapFolder.Create();
        VGMStreamFolder.Create();

        UpdateService.Initialize();

        if (AppSettings.Current.DiscordRichPresence)
        {
            DiscordService.Initialize();
        }
    }

    public void ExitHandler()
    {
        AppVM.MeshViewer?.Close();
        WindowsUtils.FreeConsole();
        AppSettings.Save();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        ExitHandler();
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error("{0}", e.Exception);

        var messageBox = new MessageBoxModel
        {
            Caption = "An unhandled exception has occurred",
            Icon = MessageBoxImage.Error,
            Text = e.Exception.Message,
            Buttons = new[] { new MessageBoxButtonModel("Reset App Settings", MessageBoxResult.Custom), new MessageBoxButtonModel("Continue", MessageBoxResult.OK) },
        };

        MessageBox.Show(messageBox);
        if (messageBox.Result == MessageBoxResult.Custom)
        {
            AppSettings.Current = new AppSettings();
            AppSettings.Save();
            AppVM.Restart();
        }

        e.Handled = true;
    }
}