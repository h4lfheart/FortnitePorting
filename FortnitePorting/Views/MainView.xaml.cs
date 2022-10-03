using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FortnitePorting.AppUtils;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;
using FortnitePorting.Views.Controls;

namespace FortnitePorting.Views;

public partial class MainView
{
    public MainView()
    {
        InitializeComponent();
        AppVM.MainVM = new MainViewModel();
        DataContext = AppVM.MainVM;

        AppLog.Logger = LoggerRtb;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(AppSettings.Current.ArchivePath))
        {
            AppHelper.OpenWindow<StartupView>();
            return;
        }
        
        await AppVM.MainVM.Initialize();
    }

    private async void OnAssetTabSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not TabControl tabControl) return;
        if (AppVM.AssetHandlerVM is null) return;
        
        var assetType = (EAssetType) tabControl.SelectedIndex;
        var handlers = AppVM.AssetHandlerVM.Handlers;
        foreach (var (handlerType, handlerData) in handlers)
        {
            if (handlerType == assetType)
            {
                handlerData.PauseState.Unpause();
            }
            else
            {
                handlerData.PauseState.Pause();
            }
        }
        
        if (!handlers[assetType].HasStarted)
        {
            await handlers[assetType].Execute();
        }
        
        DiscordService.Update(assetType);
        
    }

    private void OnAssetSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var listBox = (ListBox) sender;
        var selected = (AssetSelectorItem) listBox.SelectedItem;
        if (selected.IsRandom)
        {
            listBox.SelectedIndex = App.RandomGenerator.Next(0, listBox.Items.Count);
            return;
        }
    }
}