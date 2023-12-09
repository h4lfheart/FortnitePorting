using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Framework.Application;
using FortnitePorting.Installer.ViewModels;
using FortnitePorting.Installer.Views;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace FortnitePorting.Installer.Application;

public partial class App : AppBase
{
    public static ApplicationViewModel AppVM = null!;
    public static MainViewModel MainVM => ViewModelRegistry.Get<MainViewModel>()!;
    public static EndpointViewModel EndpointsVM => ViewModelRegistry.Get<EndpointViewModel>()!;

    public App() : base(OnStartup, OnExit)
    {
        AvaloniaXamlLoader.Load(this);
    }

    // todo admin checks
    public static void OnStartup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
            .CreateLogger();

        ViewModelRegistry.Register<EndpointViewModel>();
        
        AppVM = new ApplicationViewModel();
        MainWindow = new AppWindow();
    }

    public static void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
    }
}