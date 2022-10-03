using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using FortnitePorting.AppUtils;
using FortnitePorting.Services;
using Serilog;
using Serilog.Configuration;

namespace FortnitePorting;

public partial class App
{
    [DllImport("kernel32")]
    private static extern bool AllocConsole();
    
    [DllImport("kernel32")]
    private static extern bool FreeConsole();
    
    public static readonly DirectoryInfo AssetsFolder = new(Path.Combine(Directory.GetCurrentDirectory(), "Assets"));
    public static readonly DirectoryInfo ExportsFolder = new(Path.Combine(Directory.GetCurrentDirectory(), "Exports"));
    public static readonly DirectoryInfo DataFolder = new(Path.Combine(Directory.GetCurrentDirectory(), ".data"));

    public static readonly Random RandomGenerator = new(); 
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AllocConsole();

       Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
        
        AssetsFolder.Create();
        ExportsFolder.Create();
        DataFolder.Create();
        
        AppSettings.DirectoryPath.Create();
        AppSettings.Load();

        if (AppSettings.Current.DiscordRPC == ERichPresenceAccess.Always)
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
    }
}