using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using AdonisUI;
using AdonisUI.Controls;
using CUE4Parse.UE4.Assets;
using FortnitePorting.AppUtils;
using FortnitePorting.Exports;
using FortnitePorting.Services;
using Serilog.Sinks.SystemConsole.Themes;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;
using MessageBoxResult = AdonisUI.Controls.MessageBoxResult;

namespace FortnitePorting;

public partial class App
{
    [DllImport("kernel32")]
    private static extern bool AllocConsole();

    [DllImport("kernel32")]
    private static extern bool FreeConsole();
    
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public static DirectoryInfo AssetsFolder => new(AppSettings.Current.AssetsPath);
    public static readonly DirectoryInfo DataFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".data"));
    public static readonly DirectoryInfo MapFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Terrain"));

    public static readonly DirectoryInfo BundlesFolder = new(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
        "\\FortniteGame\\Saved\\PersistentDownloadDir\\InstalledBundles");
    public static readonly DirectoryInfo LogsFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));

    public static readonly DirectoryInfo CacheFolder = new(Path.Combine(DataFolder.FullName, "ManifestCache"));

    public static readonly Random RandomGenerator = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AllocConsole();
        Console.Title = "Fortnite Porting Console";

        ObjectTypeRegistry.RegisterEngine(typeof(UFortnitePortingCustom).Assembly);
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
            .WriteTo.File(Path.Combine(LogsFolder.FullName, $"FortnitePorting-{DateTime.UtcNow:yyyy-MM-dd-hh-mm-ss}.log"))
            .CreateLogger();

        AppSettings.DirectoryPath.Create();
        AppSettings.Load();
        
        ToggleConsole(AppSettings.Current.ShowConsole);

        ResourceLocator.SetColorScheme(Current.Resources, AppSettings.Current.LightMode ? ResourceLocator.LightColorScheme : ResourceLocator.DarkColorScheme);

        AssetsFolder.Create();
        DataFolder.Create();
        LogsFolder.Create();
        MapFolder.Create();

        UpdateService.Initialize();

        if (AppSettings.Current.DiscordRichPresence)
        {
            DiscordService.Initialize();
        }
    }

    public static void ToggleConsole(bool show)
    {
        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        
        var handle = GetConsoleWindow();
        ShowWindow(handle, show ? SW_SHOW : SW_HIDE);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        FreeConsole();
        AppSettings.Save();
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