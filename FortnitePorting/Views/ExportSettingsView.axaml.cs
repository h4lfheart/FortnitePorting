using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
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
        Navigation.ExportSettings.Initialize(NavigationView);
        Navigation.ExportSettings.AddTypeResolver<EExportLocation>(location =>
        {
            var name = location.IsFolder() ? "Folder" : location.ToString();
            var viewName = $"FortnitePorting.Views.Settings.{name}SettingsView";
        
            var type = Type.GetType(viewName);
            return type;
        });
        
        Navigation.ExportSettings.Open(EExportLocation.Blender);
        
    }
    
    private void OnItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer.Tag is not EExportLocation exportType) return;
        
        Navigation.ExportSettings.Open(exportType);
    }
}