using Avalonia;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using Avalonia.Controls.ApplicationLifetimes;
using DesktopNotifications.FreeDesktop;
using DesktopNotifications.Windows;
using FortnitePorting.Application;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using Serilog;

namespace FortnitePorting;

internal static class Program
{
    
    private static Mutex _programMutex;
    
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            _programMutex = new Mutex(true, "FortnitePortingMutex", out var isNew);

            if (isNew)
            {
                StartApp(args);
            }
            else
            {
                OpenExistingApp(args);
            }
            
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

    private static void StartApp(string[] args)
    {
        TaskService.Run(() =>
        {
            using var pipe = new NamedPipeServerStream("FortnitePorting");

            var reader = new BinaryReader(pipe);
            while (true)
            {
                pipe.WaitForConnection();
                
                while (pipe.Length == 0) { }

                var url = reader.ReadString();
                App.HandleUrlScheme(url);
                
                pipe.Disconnect();
            }
        });
        
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    
    private static void OpenExistingApp(string[] args)
    {
        using var pipe = new NamedPipeClientStream("FortnitePorting");
        pipe.Connect(1000);

        var writer = new BinaryWriter(pipe);
        writer.Write(args[0]);
    }

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<FortnitePortingApp>()
            .UsePlatformDetect()
            .SetupDesktopNotifications()
            .LogToTrace()
            .With(new Win32PlatformOptions { CompositionMode = [Win32CompositionMode.WinUIComposition] });

    private static AppBuilder SetupDesktopNotifications(this AppBuilder builder)
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