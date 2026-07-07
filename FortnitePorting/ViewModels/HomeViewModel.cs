using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Information;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Windows;

namespace FortnitePorting.ViewModels;

public partial class HomeViewModel() : ViewModelBase
{
    [ObservableProperty] private SupabaseService _supaBase;
    [ObservableProperty] private CUE4ParseService _UEParse;
    
    public HomeViewModel(SupabaseService supabase, CUE4ParseService cue4Parse) : this()
    {
        SupaBase = supabase;
        UEParse = cue4Parse;
    }
    
    [ObservableProperty] private ObservableCollection<NewsEntry> _news = [];
    [ObservableProperty] private ObservableCollection<FeaturedArtEntry> _featuredArt = [];

    public override async Task Initialize()
    {
        TaskService.Run(async () =>
        {
            var newsResponse = await Api.FortnitePorting.News();
            News = [..newsResponse.Entries.Take(3)];
            
            var featuredArtResponse = await Api.FortnitePorting.FeaturedArt();
            FeaturedArt = [..featuredArtResponse.Entries.Random(3)];
            
            await UEParse.LoadCoreSessionAsync();
        });

        if (!AppSettings.Application.DontAskAboutKofi &&
            DateTime.Now.Date >= AppSettings.Application.NextKofiAskDate)
        {
            AppSettings.Application.NextKofiAskDate = DateTime.Today.AddDays(7);
            
            Info.Dialog("Enjoying FortnitePorting?", "Consider donating to the Ko-Fi to support the development of the project!!", buttons: 
            [
                new DialogButton
                {
                    Text = "Donate",
                    Action = LaunchKoFi
                },
                new DialogButton
                {
                    Text = "Don't Ask Again",
                    Action = () => AppSettings.Application.DontAskAboutKofi = true
                }
            ]);
        }
    }

    public void OpenNews(NewsEntry news)
    {
        ChangelogWindow.Preview(news.Description);
    }

    public void OpenFeaturedArt(FeaturedArtEntry featured)
    {
        App.Launch(featured.Social);
    }
    
    public void LaunchDiscord()
    {
        App.Launch(Globals.DISCORD_URL);
    }
    
    public void LaunchTwitter()
    {
        App.Launch(Globals.TWITTER_URL);
    }
    
    public void LaunchGitHub()
    {
        App.Launch(Globals.GITHUB_URL);
    }
    
    public void LaunchKoFi()
    {
        App.Launch(Globals.KOFI_URL);
    }
    
    public void LaunchWebsite()
    {
        App.Launch(Globals.WEBSITE_URL);
    }
}