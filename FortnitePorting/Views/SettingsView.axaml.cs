using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Controls.Navigation;
using FortnitePorting.Controls.Navigation.Sidebar;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels;
using FortnitePorting.Views.Settings;

namespace FortnitePorting.Views;

public partial class SettingsView : ViewBase<SettingsViewModel>
{
    public SettingsView()
    {
        InitializeComponent();
        
        Navigation.Settings.Initialize(ContentFrame);
    }

    private void OnItemSelected(object? sender, SidebarItemSelectedArgs e)
    {
        Navigation.Settings.Open(e.Tag);
    }
}