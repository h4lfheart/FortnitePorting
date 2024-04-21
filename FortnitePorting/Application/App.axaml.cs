using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Services;
using Serilog;

namespace FortnitePorting.Application;

public partial class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new AppWindow();
            desktop.Startup += ApplicationService.OnStartup;
            ApplicationService.Application = desktop;
            Dispatcher.UIThread.UnhandledException += (sender, args) =>
            {
                args.Handled = true;

                var exceptionString = args.Exception.ToString();
                Log.Error(exceptionString);
                
                var dialog = new ContentDialog
                {
                    Title = "An unhandled exception has occurred",
                    Content = exceptionString,
                    CloseButtonText = "Continue"
                };
                dialog.ShowAsync();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
    
}