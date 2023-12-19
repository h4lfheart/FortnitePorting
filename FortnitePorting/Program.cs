using System;
using Avalonia;
using Avalonia.ReactiveUI;
using FortnitePorting.Application;
using FortnitePorting.Framework.Application;
using Serilog;

namespace FortnitePorting;

file class Program
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
            HandleException(e);
            Log.CloseAndFlush();
        }
    }

    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI()
            .With(new Win32PlatformOptions
            {
                CompositionMode = new[] { Win32CompositionMode.WinUIComposition },
                OverlayPopups = true
            })
            .With(new X11PlatformOptions
            {
                OverlayPopups = true
            })
            .With(new AvaloniaNativePlatformOptions
            {
                OverlayPopups = true
            });
    }
}