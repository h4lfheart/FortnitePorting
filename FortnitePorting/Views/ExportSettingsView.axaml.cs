using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Controls.Navigation;
using FortnitePorting.Controls.Navigation.Sidebar;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Shared;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class ExportSettingsView : ViewBase<ExportSettingsViewModel>
{
    public ExportSettingsView() : base(AppSettings.ExportSettings)
    {
        InitializeComponent();
        Navigation.ExportSettings.Initialize(Sidebar, ContentFrame);
        Navigation.ExportSettings.AddTypeResolver<EExportLocation>(location =>
        {
            var name = location.IsFolder() ? "Folder" : location.ToString();
            var viewName = $"FortnitePorting.Views.Settings.{name}SettingsView";
        
            var type = Type.GetType(viewName);
            return type;
        });
        
        Navigation.ExportSettings.Open(EExportLocation.Blender);
        
    }
    

    private void OnItemSelected(object? sender, SidebarItemSelectedArgs e)
    {
        if (e.Tag is not EExportLocation exportLocation) return;
        
        Navigation.ExportSettings.Open(exportLocation);
    }
}