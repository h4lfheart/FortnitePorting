using Avalonia;
using System;
using System.Diagnostics;
using System.Security.Principal;
using FortnitePorting.Installer.Application;

namespace FortnitePorting.Installer;

class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}