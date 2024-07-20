using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using FortnitePorting.Application;
using FortnitePorting.Models.Leaderboard;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels.Settings;
using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Rendering.RenderActions;
using ScottPlot.TickGenerators;
using ScottPlot.TickGenerators.TimeUnits;

namespace FortnitePorting.ViewModels;

public partial class LeaderboardViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<LeaderboardUser> _leaderboardUsers = [];
    [ObservableProperty] private ObservableCollection<LeaderboardExport> _leaderboardExports = [];
    [ObservableProperty] private ObservableCollection<PersonalExport> _personalExports = [];
    [ObservableProperty] private ObservableCollection<StatisticsModel> _statisticsModels = [];
    
    [ObservableProperty] private Bitmap _medalBitmap;

    [ObservableProperty] private int _popupValue;
    
    public OnlineSettingsViewModel OnlineRef => AppSettings.Current.Online;

    public override async Task OnViewOpened()
    {
        var leaderboardUsers = await ApiVM.FortnitePorting.GetLeaderboardUsersAsync();
        LeaderboardUsers = [..leaderboardUsers];
        
        var leaderboardExports = await ApiVM.FortnitePorting.GetLeaderboardExportsAsync();
        LeaderboardExports = [..leaderboardExports];
        
        var personalExports = await ApiVM.FortnitePorting.GetPersonalExportsAsync();
        PersonalExports = [..personalExports];

        var foundRankingUser = LeaderboardUsers.FirstOrDefault(user =>
            user.Identifier == AppSettings.Current.Online.Identification?.Identifier);

        if (foundRankingUser is not null)
        {
            MedalBitmap = foundRankingUser.MedalBitmap;
        }

        StatisticsModels =
        [
            new StatisticsModel("Day", TimeSpan.FromHours(1), 24, PersonalExports),
            new StatisticsModel("Week", TimeSpan.FromDays(1), 7, PersonalExports),
            new StatisticsModel("Month", TimeSpan.FromDays(1), 30, PersonalExports),
            new StatisticsModel("Year", TimeSpan.FromDays(1), DateTime.IsLeapYear(DateTime.Now.Year) ? 366 : 365, PersonalExports),
        ];
    }

    public Bitmap GetMedalBitmap(int ranking = -1)
    {
        return ImageExtensions.AvaresBitmap($"avares://FortnitePorting/Assets/FN/{ranking switch {
            1 => "GoldMedal",
            2 => "SilverMedal",
            3 => "BronzeMedal",
            _ => "NormalMedal"
        }}.png");
    }
}