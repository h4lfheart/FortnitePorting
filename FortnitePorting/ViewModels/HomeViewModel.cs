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
        if (!AppSettings.Current.Online.HasReceivedFirstPrompt)
        {
            await AppSettings.Current.Online.PromptForAuthentication();
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