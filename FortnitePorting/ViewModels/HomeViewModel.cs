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

namespace FortnitePorting.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private ObservableCollection<NewsControl> _newsControls = [];
    [ObservableProperty] private ObservableCollection<FeaturedControl> _featuredControls = [];
    
    public string DisplayName => DiscordService.GetDisplayName() ?? "No User";
    public string AvatarURL => DiscordService.GetAvatarURL() ?? "avares://FortnitePorting/Assets/DefaultProfile.png";
    public string UserName
    {
        get
        {
            var name = DiscordService.GetUserName();
            return name is not null ? $"@{name}" : "Discord RPC Disabled";
        }
    }
    
    public override async Task Initialize()
    {
        TaskService.Run(async () =>
        {
            ViewModelRegistry.Register<CUE4ParseViewModel>();
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