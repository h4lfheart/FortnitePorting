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
        var type = Type.GetType($"FortnitePorting.Views.{e.InvokedItem}View");
        ViewModel.Navigate(type);
    }
}