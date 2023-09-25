using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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
        ApplicationLifetime.MainWindow!.Width = sender is ContentControl { Content: AssetsView or LoadingView } ? 1280 : 1100; // loading view and assets view use this specifically
    }
}