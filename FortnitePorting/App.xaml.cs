using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using AdonisUI.Controls;
using CUE4Parse.UE4.Assets;
using FortnitePorting.AppUtils;
using FortnitePorting.Exports.Types;
using FortnitePorting.Services;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;

namespace FortnitePorting;

public partial class App
{
    [DllImport("kernel32")]
    private static extern bool AllocConsole();
    
    [DllImport("kernel32")]
    private static extern bool FreeConsole();

    public static readonly DirectoryInfo AssetsFolder = new(Path.Combine(Directory.GetCurrentDirectory(), "Assets"));
    public static readonly DirectoryInfo DataFolder = new(Path.Combine(Directory.GetCurrentDirectory(), ".data"));

    public static readonly DirectoryInfo BundlesFolder = new(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
        "\\FortniteGame\\Saved\\PersistentDownloadDir\\InstalledBundles");//new(Path.Combine(Directory.GetCurrentDirectory(), "InstalledBundles"));
    public static readonly DirectoryInfo LogsFolder = new(Path.Combine(Directory.GetCurrentDirectory(), "Logs"));
    
    public static readonly DirectoryInfo CacheFolder = new(Path.Combine(DataFolder.FullName, "ManifestCache"));

    public static readonly Random RandomGenerator = new(); 
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AllocConsole();
        Console.Title = "Fortnite Porting Console";
        
        ObjectTypeRegistry.RegisterEngine(typeof(FortAnimNotifyState_SpawnProp).Assembly);

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(Path.Combine(LogsFolder.FullName, $"FortnitePorting-{DateTime.UtcNow:yyyy-MM-dd-hh-mm-ss}.log"))
            .CreateLogger();
        
        AssetsFolder.Create();
        DataFolder.Create();
        LogsFolder.Create();
        
        AppSettings.DirectoryPath.Create();
        AppSettings.Load();

        if (AppSettings.Current.DiscordRichPresence)
        {
            DiscordService.Initialize();
        }
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
            Buttons = new[] {MessageBoxButtons.Ok()}
        };
        MessageBox.Show(messageBox);

        e.Handled = true;
    }
}