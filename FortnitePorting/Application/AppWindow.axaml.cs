using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels;
using FortnitePorting.Views;

namespace FortnitePorting.Application;

public partial class AppWindow : WindowBase<AppViewModel>
{
    public AppWindow()
    {
        InitializeComponent();
        DataContext = ViewModel;
        ViewModel.ContentFrame = ContentFrame;
        ViewModel.NavigationView = NavigationView;

        if (AppSettings.Current.FinishedWelcomeScreen)
        {
            ViewModel.Navigate<HomeView>();
        }
        else
        {
            ViewModel.Navigate<WelcomeView>();
        }
    }

    private void OnItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        var type = Type.GetType($"FortnitePorting.Views.{e.InvokedItem}View");
        ViewModel.Navigate(type);
    }
}