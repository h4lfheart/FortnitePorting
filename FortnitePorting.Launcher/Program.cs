using System;
using System.IO.Pipes;
using System.Threading;
using Avalonia;
using FortnitePorting.Launcher.Application;
using FortnitePorting.Shared.Services;
using Serilog;

namespace FortnitePorting.Launcher;

internal static class Program
{
    private static Mutex _programMutex;
    
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            _programMutex = new Mutex(true, "FortnitePortingLauncherMutex", out var isNew);
            
            if (isNew)
            {
                StartNewApp(args);
            }
            else
            {
                OpenExistingApp();
            }
        }
        catch (Exception e)
        {
            Log.Fatal(e.ToString());
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static void StartNewApp(string[] args)
    {
        using var pipe = new NamedPipeServerStream("FortnitePortingLauncher");
        
        var responseThread = new Thread(() =>
        {
            while (true)
            {
                pipe.WaitForConnection();
            
                TaskService.RunDispatcher(OpenAppWindow);
                
                pipe.Disconnect();
            }
        });

        responseThread.IsBackground = true;
        responseThread.Start();
        
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    
    public static void OpenExistingApp()
    {
        using var pipe = new NamedPipeClientStream("FortnitePortingLauncher");
        pipe.Connect(1000);
    }
    
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .With(new Win32PlatformOptions { CompositionMode = [Win32CompositionMode.WinUIComposition] });
}