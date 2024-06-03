using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Controls;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using Serilog;
using FeaturedControl = FortnitePorting.Controls.Home.FeaturedControl;
using NewsControl = FortnitePorting.Controls.Home.NewsControl;

namespace FortnitePorting.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private ObservableCollection<NewsControl> _newsControls = [];
    [ObservableProperty] private ObservableCollection<FeaturedControl> _featuredControls = [];

    [ObservableProperty] private string _displayName;
    [ObservableProperty] private string _userName;
    [ObservableProperty] private string _avatarURL;
    
    public override async Task Initialize()
    {
        // TODO adhere to options in AppSettings
        TaskService.Run(() =>
        {
            DiscordService.Client!.OnReady += (sender, args) =>
            {
                var name = DiscordService.GetUserName();
                UserName = name is not null ? $"@{name}" : "Discord RPC Disabled";

                DisplayName = DiscordService.GetDisplayName() ?? "No User";
                AvatarURL = DiscordService.GetAvatarURL() ?? "avares://FortnitePorting/Assets/DefaultProfile.png";
            };
        });
        
        TaskService.Run(async () =>
        {
            ViewModelRegistry.New<CUE4ParseViewModel>();
            await CUE4ParseVM.Initialize();

            AppVM.GameBasedTabsAreReady = true;
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
        return;
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