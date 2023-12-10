using Avalonia;
using System;
using System.Diagnostics;
using System.Security.Principal;
using FortnitePorting.Installer.Application;

namespace FortnitePorting.Installer;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (HasAdmin())
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        else
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Environment.ProcessPath,
                UseShellExecute = true,
                Verb = "runas"
            });
        }
    }
    
    private static bool HasAdmin()
    {
        var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}