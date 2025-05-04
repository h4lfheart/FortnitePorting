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

namespace FortnitePorting.Views;

public partial class ExportSettingsView : ViewBase<ExportSettingsViewModel>
{
    public ExportSettingsView() : base(AppSettings.ExportSettings)
    {
        InitializeComponent();
        Navigation.ExportSettings.Initialize(NavigationView);
        Navigation.ExportSettings.Open(EExportLocation.Blender.SettingsViewType());
        
    }
    
    private void OnItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer.Tag is not EExportLocation exportType) return;
        
        Navigation.ExportSettings.Open(exportType.SettingsViewType());
    }
}