using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class ExportSettingsView : ViewBase<ExportSettingsViewModel>
{
    public ExportSettingsView() : base(AppSettings.Current.ExportSettings)
    {
        InitializeComponent();
        ViewModel.ContentFrame = ContentFrame;
        ViewModel.NavigationView = NavigationView;
        ViewModel.Navigate(EExportLocation.Blender);
    }
    
    private void OnItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer.Tag is not EExportLocation exportType) return;

        ViewModel.Navigate(exportType);
    }
}