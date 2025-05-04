using Avalonia;
using System;
using System.Diagnostics;
using Avalonia.Controls.ApplicationLifetimes;
using DesktopNotifications.FreeDesktop;
using DesktopNotifications.Windows;
using FortnitePorting.Application;
using Serilog;

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
            Debugger.Break();
            Log.Fatal(e.ToString());
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<FortnitePortingApp>()
            .UsePlatformDetect()
            .SetupDesktopNotifications()
            .LogToTrace()
            .With(new Win32PlatformOptions { CompositionMode = [Win32CompositionMode.WinUIComposition] });
    
    public static AppBuilder SetupDesktopNotifications(this AppBuilder builder)
    {
        try
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var context = WindowsApplicationContext.FromCurrentProcess();
                App.NotificationManager = new WindowsNotificationManager(context);
            }
            else if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                var context = FreeDesktopApplicationContext.FromCurrentProcess();
                App.NotificationManager = new FreeDesktopNotificationManager(context);
            }
            else
            {
                App.NotificationManager = null;
                return builder;
            }

            App.NotificationManager.Initialize().GetAwaiter().GetResult();

            builder.AfterSetup(b =>
            {
                if (b.Instance?.ApplicationLifetime is IControlledApplicationLifetime lifetime)
                {
                    lifetime.Exit += (s, e) => { App.NotificationManager.Dispose(); };
                }
            });

        }
        catch (Exception)
        {
            Log.Error("Failed to setup notification manager.");
        }
        
        return builder;
    }

}