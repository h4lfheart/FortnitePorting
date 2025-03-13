using System;
using Avalonia.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Launcher.WindowModels;
using FortnitePorting.Shared.Framework;

namespace FortnitePorting.Launcher.Windows;

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
        var viewName = $"FortnitePorting.Launcher.Views.{e.InvokedItemContainer.Tag}View";
        
        var type = Type.GetType(viewName);
        if (type is null) return;
        
        WindowModel.Navigate(type);
    }

    private void OnPointerPressedUpperBar(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }
}