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
        Navigation.Plugin.Open(EExportLocation.Blender.PluginViewType());
    }
    
    private void OnItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer.Tag is not EExportLocation exportType) return;

        Navigation.Plugin.Open(exportType.PluginViewType());
    }
}