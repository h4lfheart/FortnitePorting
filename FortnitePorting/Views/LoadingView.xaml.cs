using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AdonisUI.Controls;
using FortnitePorting.AppUtils;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;
using OpenTK.Graphics.OpenGL;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;
using MessageBoxResult = AdonisUI.Controls.MessageBoxResult;

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
        if (AppSettings.Current.IsFirstStartup)
        {
            var messageBox = new MessageBoxModel
            {
                Text = "Would you like to opt into data collection?\nThis only tracks the daily users on Fortnite Porting V1, everything is anonymous.\nYour installation UUID and Version are the only data sent over.",
                Caption = "Data Collection",
                Icon = MessageBoxImage.Information,
                Buttons = MessageBoxButtons.YesNo(),
                IsSoundEnabled = false
            };

            MessageBox.Show(messageBox);
            if (messageBox.Result == MessageBoxResult.Yes)
            {
                AppSettings.Current.AllowingDataCollection = true;
            }
            
            AppSettings.Current.IsFirstStartup = false;
        }

        if (AppSettings.Current.AllowingDataCollection)
        {
            await EndpointService.FortnitePorting.PostStatsAsync();
        }
        
        if (string.IsNullOrWhiteSpace(AppSettings.Current.ArchivePath) && AppSettings.Current.InstallType == EInstallType.Local)
        {
            AppHelper.OpenWindow<StartupView>();
            return;
        }

        AppVM.LoadingVM.Update("Checking For Updates");
        var broadcasts = await EndpointService.FortnitePorting.GetBroadcastsAsync();
        var validBroadcasts = broadcasts.Where(broadcast => broadcast.Version.Equals(Globals.VERSION) || broadcast.Version.Equals("All"));
        foreach (var broadcast in validBroadcasts)
        {
            if (broadcast.PushedTime <= AppSettings.Current.LastBroadcastTime || !broadcast.IsActive) continue;
            AppSettings.Current.LastBroadcastTime = broadcast.PushedTime;

            var messageBox = new MessageBoxModel
            {
                Caption = broadcast.Title,
                Text = broadcast.Contents,
                Icon = MessageBoxImage.Exclamation,
                Buttons = new[] { MessageBoxButtons.Ok() }
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

        await AppVM.LoadingVM.Initialize();
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        
        DragMove();
    }

    private void OpenSettings(object sender, MouseButtonEventArgs e)
    {
        AppHelper.OpenWindow<SettingsView>(this);
    }
}