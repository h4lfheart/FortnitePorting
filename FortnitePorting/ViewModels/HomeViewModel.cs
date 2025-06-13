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
using FortnitePorting.Models.Supabase.Tables;


using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.ViewModels.Settings;
using Serilog;
using Supabase.Postgrest;

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
    
    [ObservableProperty] private ObservableCollection<NewsResponse> _news = [];
    [ObservableProperty] private ObservableCollection<FeaturedArtResponse> _featuredArt = [];

    public override async Task Initialize()
    {
        TaskService.Run(async () =>
        {
            News = [..await Api.FortnitePorting.News()];
            FeaturedArt = [..await Api.FortnitePorting.FeaturedArt()];
            
            await UEParse.Initialize();
            await FilesVM.Initialize();
        });
        

        if (!AppSettings.Application.DontAskAboutKofi &&
            DateTime.Now.Date >= AppSettings.Application.NextKofiAskDate)
        {
            AppSettings.Application.NextKofiAskDate = DateTime.Today.AddDays(7);
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
                    SecondaryButtonCommand = new RelayCommand(() => AppSettings.Application.DontAskAboutKofi = true)
                };

                await dialog.ShowAsync();
            });
        }
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
}