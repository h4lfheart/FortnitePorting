using System.Linq;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DynamicData;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
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
        BindingPlugins.DataValidators.RemoveAll(validator => validator is DataAnnotationsValidationPlugin);
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            ApplicationService.Application = desktop;
            ApplicationService.Initialize();
        }

        base.OnFrameworkInitializationCompleted();
    }
    
}