using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Controls.Navigation;
using FortnitePorting.Controls.Navigation.Sidebar;
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
        Navigation.Plugin.Initialize(Sidebar, ContentFrame);
        Navigation.Plugin.AddTypeResolver<EExportLocation>(location =>
        {
            var name = location.ToString();
            var viewName = $"FortnitePorting.Views.Plugin.{name}PluginView";
        
            var type = Type.GetType(viewName);
            return type;
        });
    }
    
    private void OnItemSelected(object? sender, SidebarItemSelectedArgs e)
    {
        if (e.Tag is not EExportLocation exportLocation) return;

        Navigation.Plugin.Open(exportLocation);
    }
}