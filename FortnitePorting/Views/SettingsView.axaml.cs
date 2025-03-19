using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels;
using FortnitePorting.Views.Settings;

namespace FortnitePorting.Views;

public partial class SettingsView : ViewBase<SettingsViewModel>
{
    public SettingsView()
    {
        InitializeComponent();
        ViewModel.ContentFrame = ContentFrame;
        ViewModel.NavigationView = NavigationView;
        ViewModel.Navigate<ApplicationSettingsView>();
    }

    private void OnItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer.Tag is not string tagName) return;
        
        if (tagName == "Reset")
        {
            RestartWithMessage("A restart is required", "To reset all settings, FortnitePorting must be restarted.", AppSettings.Reset);
            return;
        }
        
        var viewName = $"FortnitePorting.Views.Settings.{tagName}SettingsView";
        
        var type = Type.GetType(viewName);
        if (type is null) return;
        
        ViewModel.Navigate(type);
    }
}