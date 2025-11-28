using Avalonia;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using FortnitePorting.Application;
using FortnitePorting.Services;
using Serilog;

namespace FortnitePorting;

internal static class Program
{
    private static Mutex _programMutex = null!;
    
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
            _programMutex.ReleaseMutex();
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

                var url = reader.ReadString();
                App.HandleUrlScheme(url);
                
                pipe.Disconnect();
            }
        });
        
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    
    private static void OpenExistingApp(string[] args)
    {
        try
        {
            using var pipe = new NamedPipeClientStream("FortnitePorting");
            pipe.Connect(1000);

            var writer = new BinaryWriter(pipe);
            writer.Write(args[0]);
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            StartApp(args);
        }
    }

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<FortnitePortingApp>()
            .UsePlatformDetect()
            .LogToTrace()
            .With(new Win32PlatformOptions { CompositionMode = [Win32CompositionMode.WinUIComposition] });
}