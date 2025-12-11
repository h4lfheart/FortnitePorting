using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.OnlineServices.Models;
using FortnitePorting.OnlineServices.Packet;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Models;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels.Settings;
using Serilog;

namespace FortnitePorting.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private ObservableCollection<NewsResponse> _news = [];
    [ObservableProperty] private ObservableCollection<FeaturedResponse> _featured = [];

    public OnlineSettingsViewModel OnlineRef => AppSettings.Current.Online;
    
    public override async Task Initialize()
    {
        if (!AppSettings.Current.Application.HasReceivedWinterBGMPrompt)
        {
            await TaskService.RunDispatcherAsync(async () =>
            {
                var dialog = new ContentDialog
                {
                    Title = "Winter Theme",
                    Content = "Would you like to enable the Winter background music? (This can be changed any time in theme settings)",
                    CloseButtonText = "No",
                    PrimaryButtonText = "Yes",
                    PrimaryButtonCommand = new RelayCommand(() => AppSettings.Current.Theme.UseWinterBGM = true)
                };

                await dialog.ShowAsync();

                AppSettings.Current.Application.HasReceivedWinterBGMPrompt = true;
            });
        }
        
        if (!AppSettings.Current.Online.HasReceivedFirstPrompt)
        {
            await AppSettings.Current.Online.PromptForAuthentication();
        }

        if (!AppSettings.Current.Application.DontAskAboutKofi &&
            DateTime.Now.Date >= AppSettings.Current.Application.NextKofiAskDate)
        {
            AppSettings.Current.Application.NextKofiAskDate = DateTime.Today.AddDays(7);
            await TaskService.RunDispatcherAsync(async () =>
            {
                var dialog = new ContentDialog
                {
                    Title = "Enjoying FortnitePorting?",
                    Content = "Consider donating to the Ko-Fi to support the development of the project!!",
                    CloseButtonText = "No",
                    PrimaryButtonText = "Donate",
                    PrimaryButtonCommand = new RelayCommand(LaunchKoFi),
                    SecondaryButtonText = "Don't Ask Again",
                    SecondaryButtonCommand = new RelayCommand(() => AppSettings.Current.Application.DontAskAboutKofi = true)
                };

                await dialog.ShowAsync();
            });
        }
        
        TaskService.Run(async () =>
        {
            ViewModelRegistry.New<CUE4ParseViewModel>();
            await CUE4ParseVM.Initialize();
            
            AppWM.GameBasedTabsAreReady = true;
            AppWM.OnlineAndGameTabsAreVisible = AppSettings.Current.Online.UseIntegration;
            
            ViewModelRegistry.New<FilesViewModel>();
            await FilesVM.Initialize();
        });
        
        var news = await ApiVM.FortnitePorting.GetNewsAsync();
        News = [..news.OrderByDescending(item => item.Date)];
        
        var featured = await ApiVM.FortnitePorting.GetFeaturedAsync();
        Featured = [..featured];
    }
    
    public void UpdateStatus(string text)
    {
        StatusText = text;
    }
    
    public void LaunchDiscord()
    {
        Launch(Globals.DISCORD_URL);
    }
    
    public void LaunchTwitter()
    {
        Launch(Globals.TWITTER_URL);
    }
    
    public void LaunchGitHub()
    {
        Launch(Globals.GITHUB_URL);
    }
    
    public void LaunchKoFi()
    {
        Launch(Globals.KOFI_URL);
    }
}