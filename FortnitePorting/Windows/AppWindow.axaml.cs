using System;
using Avalonia.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;
using AppWindowModel = FortnitePorting.WindowModels.AppWindowModel;

namespace FortnitePorting.Windows;

public partial class AppWindow : WindowBase<AppWindowModel>
{
    public AppWindow() : base(initializeWindowModel: false)
    {
        InitializeComponent();
        DataContext = WindowModel;
        WindowModel.ContentFrame = ContentFrame;
        WindowModel.NavigationView = NavigationView;
    }

    private void OnItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        var viewName = $"FortnitePorting.Views.{e.InvokedItemContainer.Tag}View";
        
        var type = Type.GetType(viewName);
        if (type is null) return;
        
        WindowModel.Navigate(type);
    }

    private async void OnUpdatePressed(object? sender, PointerPressedEventArgs e)
    {
        await WindowModel.CheckForUpdate();
    }
}