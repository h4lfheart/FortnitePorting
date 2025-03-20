using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FortnitePorting.Launcher.Services;
using FortnitePorting.Launcher.Windows;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Launcher.Application;

public partial class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        BindingPlugins.DataValidators.RemoveAll(validator => validator is DataAnnotationsValidationPlugin);
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            ApplicationService.Application = desktop;
            ApplicationService.Initialize();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnTrayIconOpen(object? sender, EventArgs e)
    {
        OpenAppWindow();
    }

    private void OnTrayIconQuit(object? sender, EventArgs eventArgs)
    {
        ApplicationService.Application.Shutdown();
    }
}