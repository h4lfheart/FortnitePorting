using System;
using Avalonia.Controls;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Framework.Services;
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

            TaskService.Run(async () =>
            {
                await AssetsVM.Initialize();
                ViewModel.AssetTabReady = true;
            });

            TaskService.Run(async () =>
            {
                await FilesVM.Initialize();
                ViewModel.MeshTabReady = true;
            });

            ViewModel.RadioTabReady = true;
        });
    }

    private void OnTabChanged(object? sender, EventArgs e)
    {
        ViewModel.ActiveTab = (UserControl) sender!;
        MainWindow.Width = ViewModel.ActiveTab is AssetsView or FilesView or RadioView && MainWindow.WindowState == WindowState.Normal ? 1280 : 1100; // assets view and files view use this specifically
        MainWindow.UpdateLayout();

        if (AppSettings.Current.IsRestartRequired)
        {
            AppVM.RestartWithMessage("A restart is required.", "An option has been changed that requires a restart to take effect.");
            AppSettings.Current.IsRestartRequired = false;
        }
    }
}