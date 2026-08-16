using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Information;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Windows;

namespace FortnitePorting.ViewModels;

public partial class HomeViewModel(
    CUE4ParseService ueParse,
    APIService api,
    SettingsService settings,
    InfoService info,
    AppService app) : ViewModelBase
{
    [ObservableProperty] private CUE4ParseService _UEParse = ueParse;

    private readonly APIService _api = api;
    private readonly SettingsService _settings = settings;
    private readonly InfoService _info = info;
    private readonly AppService _app = app;

    [ObservableProperty] private ObservableCollection<NewsEntry> _news = [];
    [ObservableProperty] private ObservableCollection<FeaturedArtEntry> _featuredArt = [];

    public override async Task Initialize()
    {
        TaskService.Run(async () =>
        {
            var newsResponse = await _api.FortnitePorting.News();
            News = [..newsResponse.Entries.OrderByDescending(entry => entry.Date)];

            var featuredArtResponse = await _api.FortnitePorting.FeaturedArt();
            var featured = featuredArtResponse.Entries.ToList();
            featured.Shuffle();
            FeaturedArt = [..featured];

            await _UEParse.LoadCoreSessionAsync();
        });

        if (!_settings.Application.DontAskAboutKofi &&
            DateTime.Now.Date >= _settings.Application.NextKofiAskDate)
        {
            _settings.Application.NextKofiAskDate = DateTime.Today.AddDays(7);

            _info.Dialog("Enjoying FortnitePorting?", "Consider donating to the Ko-Fi to support the development of the project!!", buttons:
            [
                new DialogButton
                {
                    Text = "Donate",
                    IsPrimary = true,
                    Action = LaunchKoFi
                },
                new DialogButton
                {
                    Text = "Don't Ask Again",
                    Action = () => _settings.Application.DontAskAboutKofi = true
                },
                
                new DialogButton
                {
                    Text = "Remind Me Later"
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
        _app.Launch(featured.Social);
    }

    public void LaunchDiscord() => _app.Launch(Globals.DISCORD_URL);
    public void LaunchTwitter() => _app.Launch(Globals.TWITTER_URL);
    public void LaunchGitHub() => _app.Launch(Globals.GITHUB_URL);
    public void LaunchKoFi() => _app.Launch(Globals.KOFI_URL);
    public void LaunchWebsite() => _app.Launch(Globals.WEBSITE_URL);
}