using Avalonia.Controls.ApplicationLifetimes;

namespace FortnitePorting.Application;

public class App : Avalonia.Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new AppWindow
            {
                DataContext = AppVM
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}