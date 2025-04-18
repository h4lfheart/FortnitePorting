using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Shared;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class PluginView : ViewBase<PluginViewModel>
{
    public PluginView() : base(AppSettings.Current.Plugin)
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