using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class MainView : ViewBase<MainViewModel>
{
    public MainView()
    {
        InitializeComponent();
        
        TaskService.Run(async () =>
        {
            ViewModelRegistry.Register<CUE4ParseViewModel>();
            await CUE4ParseVM.Initialize();
            
            TaskService.Run(AssetsVM.Initialize);
            ViewModel.AssetTabReady = true;
            
            await MeshesVM.Initialize();
            ViewModel.MeshTabReady = true;
        });
    }

    private void OnTabChanged(object? sender, EventArgs e)
    {
        ViewModel.ActiveTab = (UserControl) sender!;
        MainWindow.Width = ViewModel.ActiveTab is AssetsView or MeshesView && MainWindow.WindowState == WindowState.Normal ? 1280 : 1100; // assets view and meshes view use this specifically
        MainWindow.UpdateLayout();

        if (AppSettings.Current.IsRestartRequired)
        {
            AppVM.RestartWithMessage("A restart is required.", "An option has been changed that requires a restart to take effect.");
            AppSettings.Current.IsRestartRequired = false;
        }
    }
}