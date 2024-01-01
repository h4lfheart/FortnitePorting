using System;
using Avalonia.Controls;
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
        ViewModel.ActiveTab = (UserControl) sender!;
        MainWindow.Width = ViewModel.ActiveTab is AssetsView or FilesView or RadioView && MainWindow.WindowState == WindowState.Normal ? 1280 : 1100; // assets view and files view use this specifically
        MainWindow.UpdateLayout();
    }
}