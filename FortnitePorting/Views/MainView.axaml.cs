using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class MainView : ViewBase<MainViewModel>
{
    public MainView()
    {
        InitializeComponent();
    }

    private void OnTabChanged(object? sender, EventArgs e)
    {
        if (sender is ContentControl { Content: AssetsView or LoadingView } control)
        {
            ViewModel.ActiveTab = (UserControl) control.Content;
        }
        else
        {
            ViewModel.ActiveTab = (UserControl) sender!;
        }

        ApplicationLifetime.MainWindow.Width = ViewModel.ActiveTab is AssetsView or LoadingView ? 1280 : 1100; // loading view and assets view use this specifically

        if (AppSettings.Current.IsRestartRequired)
        {
            AppVM.RestartWithMessage("A restart is required.", "An option has been changed that requires a restart to take effect.");
            AppSettings.Current.IsRestartRequired = false;
        }
    }
}