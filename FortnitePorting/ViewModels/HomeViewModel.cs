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
using FortnitePorting.OnlineServices.Models;
using FortnitePorting.OnlineServices.Packet;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Models;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels.Settings;
using Serilog;
using FeaturedControl = FortnitePorting.Controls.Home.FeaturedControl;
using NewsControl = FortnitePorting.Controls.Home.NewsControl;

namespace FortnitePorting.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private ObservableCollection<NewsControl> _newsControls = [];
    [ObservableProperty] private ObservableCollection<FeaturedControl> _featuredControls = [];

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
            
            ViewModelRegistry.New<FilesViewModel>();
            await FilesVM.Initialize();
        });
        
        
        await TaskService.RunDispatcherAsync(async () =>
        {
            var news = await ApiVM.FortnitePorting.GetNewsAsync();
            if (news is null) return;

            var controls = news
                .OrderByDescending(item => item.Date)
                .Select(item => new NewsControl(item));
            NewsControls = [..controls];
        });
        
        await TaskService.RunDispatcherAsync(async () =>
        {
            var featured = await ApiVM.FortnitePorting.GetFeaturedAsync();
            if (featured is null) return;

            var controls = featured.Select(item => new FeaturedControl(item));
            FeaturedControls = [..controls];
        });
    }
    
    public void UpdateStatus(string text)
    {
        StatusText = text;
    }
    
    public void LaunchWiki()
    {
        throw new NotImplementedException("Wiki has not been created yet.");
    }

    public void LaunchDiscord()
    {
        Launch(Globals.DISCORD_URL);
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