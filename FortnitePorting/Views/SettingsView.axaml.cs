using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
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
        var viewName = $"FortnitePorting.Views.Settings.{e.InvokedItemContainer.Tag}SettingsView";
        
        var type = Type.GetType(viewName);
        if (type is null) return;
        
        ViewModel.Navigate(type);
    }
}