using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Shared;
using FortnitePorting.ViewModels;
using FortnitePorting.Views.Plugin;

namespace FortnitePorting.Views;

public partial class PluginView : ViewBase<PluginViewModel>
{
    public PluginView() : base(AppSettings.Plugin)
    {
        InitializeComponent();
        Navigation.Plugin.Initialize(NavigationView);
        Navigation.Plugin.AddTypeResolver<EExportLocation>(location =>
        {
            var name = location.ToString();
            var viewName = $"FortnitePorting.Views.Plugin.{name}PluginView";
        
            var type = Type.GetType(viewName);
            return type;
        });
        
        Navigation.Plugin.Open(EExportLocation.Blender);
    }
    
    private void OnItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer.Tag is not EExportLocation exportType) return;

        Navigation.Plugin.Open(exportType);
    }
}