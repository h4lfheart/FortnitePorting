using System;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels;
using AppWindowModel = FortnitePorting.WindowModels.AppWindowModel;

namespace FortnitePorting.Windows;

public partial class AppWindow : WindowBase<AppWindowModel>
{
    public AppWindow()
    {
        InitializeComponent();
        DataContext = ViewModel;
        ViewModel.ContentFrame = ContentFrame;
        ViewModel.NavigationView = NavigationView;
    }

    private void OnItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        var viewName = $"FortnitePorting.Views.{e.InvokedItemContainer.Tag}View";
        
        var type = Type.GetType(viewName);
        if (type is null) return;
        
        ViewModel.Navigate(type);
    }
}