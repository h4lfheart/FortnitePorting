using System;
using System.Collections.Generic;
using System.Linq;
using AssetRipper.TextureDecoder.Rgb.Channels;
using Avalonia.Controls;
using Avalonia.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Controls.Navigation;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;
using FortnitePorting.Views;
using AppWindowModel = FortnitePorting.WindowModels.AppWindowModel;

namespace FortnitePorting.Windows;

public partial class AppWindow : WindowBase<AppWindowModel>
{
    public AppWindow() : base(initializeWindowModel: false)
    {
        InitializeComponent();
        DataContext = WindowModel;
        
        Navigation.App.Initialize(Sidebar, ContentFrame);
    }

    private void OnSidebarItemSelected(object? sender, SidebarItemSelectedArgs args)
    {
        Navigation.App.Open(args.Tag);
    }
}