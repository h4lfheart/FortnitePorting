using System;
using System.Collections.Generic;
using System.Linq;
using AssetRipper.TextureDecoder.Rgb.Channels;
using Avalonia.Controls;
using Avalonia.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Framework;
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
        
        Navigation.App.Initialize(NavigationView);
        
        KeyDownEvent.AddClassHandler<TopLevel>((sender, args) => BlackHole.HandleKey(args.Key), handledEventsToo: true);
    }

    private void OnItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItemContainer.Tag is not Type type) return;
        
        Navigation.App.Open(type);
    }

    private async void OnUpdatePressed(object? sender, PointerPressedEventArgs e)
    {
        await WindowModel.CheckForUpdate();
    }

    private void OnPointerPressedUpperBar(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void OnMinimizePressed(object? sender, PointerPressedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }
    
    private void OnMaximizePressed(object? sender, PointerPressedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }
    
    private void OnClosePressed(object? sender, PointerPressedEventArgs e)
    {
        Close();
    }
}