using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.ReactiveUI;
using FortnitePorting.Application;

namespace FortnitePorting.Desktop;

class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI()
            .With(new Win32PlatformOptions
            {
                CompositionMode = new [] { Win32CompositionMode.WinUIComposition }
            });
}