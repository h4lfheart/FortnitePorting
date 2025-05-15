using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels;
using FortnitePorting.Views.Settings;

namespace FortnitePorting.Views;

public partial class SettingsView : ViewBase<SettingsViewModel>
{
    public SettingsView()
    {
        InitializeComponent();
        
        Navigation.Settings.Initialize(NavigationView);
        Navigation.Settings.Open<ApplicationSettingsView>();
    }

    private void OnItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        switch (e.InvokedItemContainer.Tag)
        {
            case Type type:
            {
                Navigation.Settings.Open(type);
                break;
            }
            case string stringTag:
            {
                if (stringTag.Equals("Reset"))
                {
                    App.RestartWithMessage("A restart is required", "To reset all settings, FortnitePorting must be restarted.", AppSettings.Reset);
                }
                
                break;
            }
        }
    }
}