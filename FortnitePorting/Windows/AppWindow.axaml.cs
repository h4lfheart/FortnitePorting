using System;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Windows;

public partial class AppWindow : WindowBase<AppViewModel>
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
        var viewName = $"FortnitePorting.Views.{e.InvokedItem}View";
        
        var type = Type.GetType(viewName);
        if (type is null) return;
        
        ViewModel.Navigate(type);
    }
}