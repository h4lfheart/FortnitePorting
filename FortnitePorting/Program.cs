using Avalonia;
using System;
using ATL.Logging;
using Avalonia.Controls.ApplicationLifetimes;
using DesktopNotifications;
using DesktopNotifications.FreeDesktop;
using DesktopNotifications.Windows;
using FortnitePorting.Application;
using FortnitePorting.Services;
using Log = Serilog.Log;

namespace FortnitePorting;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            Log.CloseAndFlush();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .SetupDesktopNotifications()
            .LogToTrace()
            .With(new Win32PlatformOptions { CompositionMode = [Win32CompositionMode.WinUIComposition] });
    
    public static AppBuilder SetupDesktopNotifications(this AppBuilder builder)
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            var context = WindowsApplicationContext.FromCurrentProcess();
            NotificationManager = new WindowsNotificationManager(context);
        }
        else if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            var context = FreeDesktopApplicationContext.FromCurrentProcess();
            NotificationManager = new FreeDesktopNotificationManager(context);
        }
        else
        {
            NotificationManager = null;
            return builder;
        }

        NotificationManager.Initialize().GetAwaiter().GetResult();

        builder.AfterSetup(b =>
        {
            if (b.Instance?.ApplicationLifetime is IControlledApplicationLifetime lifetime)
            {
                lifetime.Exit += (s, e) => { NotificationManager.Dispose(); };
            }
        });

        return builder;
    }

}