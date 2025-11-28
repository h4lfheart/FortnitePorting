using System;
using System.IO;
using System.Linq;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using FluentAvalonia.Styling;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Application;

public partial class FortnitePortingApp : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        BindingPlugins.DataValidators.RemoveAll(validator => validator is DataAnnotationsValidationPlugin);

        if (Styles.OfType<FluentAvaloniaTheme>().FirstOrDefault() is { } fluentTheme)
        {
            fluentTheme.CustomAccentColor = Color.Parse("#303030");
        }
        
        AppServices.Initialize();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            App.InitializeDesktop(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }
    
}