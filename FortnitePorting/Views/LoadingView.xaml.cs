using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AdonisUI.Controls;
using FortnitePorting.AppUtils;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;

namespace FortnitePorting.Views;

public partial class LoadingView
{
    public LoadingView()
    {
        InitializeComponent();
        AppVM.LoadingVM = new LoadingViewModel();
        DataContext = AppVM.LoadingVM;

        AppVM.LoadingVM.TitleText = Globals.TITLE;
        Title = Globals.TITLE;
    }
    
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(AppSettings.Current.ArchivePath) && AppSettings.Current.InstallType == EInstallType.Local)
        {
            AppHelper.OpenWindow<StartupView>();
            return;
        }

        AppVM.LoadingVM.Update("Checking For Updates");
        var broadcast = await EndpointService.FortnitePorting.GetBroadcastAsync();
        if (broadcast?.PushedTime > AppSettings.Current.LastBroadcastTime && broadcast.IsActive)
        {
            AppSettings.Current.LastBroadcastTime = broadcast.PushedTime;
            
            var messageBox = new MessageBoxModel
            {
                Caption = broadcast.Title,
                Text = broadcast.Contents,
                Icon = MessageBoxImage.Exclamation,
                Buttons = new []{ MessageBoxButtons.Ok() }
            };

            MessageBox.Show(messageBox);
        }
        
        var (updateAvailable, updateVersion) = UpdateService.GetStats();
        if (DateTime.Now >= AppSettings.Current.LastUpdateAskTime.AddDays(1) || updateVersion > AppSettings.Current.LastKnownUpdateVersion)
        {
            AppSettings.Current.LastKnownUpdateVersion = updateVersion;
            UpdateService.Start(automaticCheck: true);
            AppSettings.Current.LastUpdateAskTime = DateTime.Now;
        }

        if (AppSettings.Current.JustUpdated && !updateAvailable)
        {
            AppHelper.OpenWindow<PluginUpdateView>();
            AppSettings.Current.JustUpdated = false;
        }

        await AppVM.LoadingVM.Initialize();
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }
}