global using static FortnitePorting.Services.ApplicationService;
using Avalonia;
using System;
using System.Collections.ObjectModel;
using FortnitePorting.Application;

namespace FortnitePorting;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .With(new Win32PlatformOptions { CompositionMode = [Win32CompositionMode.WinUIComposition] });
}