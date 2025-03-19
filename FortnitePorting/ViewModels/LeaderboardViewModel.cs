using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using FortnitePorting.Application;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Leaderboard;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
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
    [ObservableProperty] private ObservableCollection<LeaderboardStreaksUser> _leaderboardStreaks = [];
    [ObservableProperty] private ObservableCollection<PersonalExport> _personalExports = [];
    [ObservableProperty] private ObservableCollection<StatisticsModel> _statisticsModels = [];
    [ObservableProperty] private StreaksResponse? _streaksResponse;

    [ObservableProperty] private Bitmap? _medalBitmap;

    [ObservableProperty] private int _popupValue;
    
    public OnlineSettingsViewModel OnlineRef => AppSettings.Current.Online;

    public override async Task OnViewOpened()
    {
        if (Design.IsDesignMode) return;
        
        TaskService.Run(Load);
        
        var personalExports = await ApiVM.FortnitePorting.GetPersonalExportsAsync();
        PersonalExports = [..personalExports];
        
        var leaderboardStreaks = await ApiVM.FortnitePorting.GetStreaksAsync();
        LeaderboardStreaks = [..leaderboardStreaks];

        StreaksResponse = await ApiVM.FortnitePorting.GetPersonalStreaksAsync();
        
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

    public async Task Load() 
    {
        var leaderboardUsers = (await ApiVM.FortnitePorting.GetLeaderboardUsersAsync()).ToList();
        var leaderboardExports = (await ApiVM.FortnitePorting.GetLeaderboardExportsAsync()).ToList();
        var invalidExportsByUser = new Dictionary<Guid, int>();
        foreach (var export in leaderboardExports)
        {
            var isValid = await export.Load();
            if (isValid) continue;
                
            foreach (var (guid, count) in export.Contributions)
            {
                invalidExportsByUser.TryAdd(guid, 0);
                invalidExportsByUser[guid] += count;
            }
        }

        foreach (var (guid, count) in invalidExportsByUser)
        {
            var targetUser = leaderboardUsers.FirstOrDefault(user => user.Identifier == guid);
            if (targetUser is null) continue;

            var offsetCount = targetUser.ExportCount - count;
            if (offsetCount <= 0)
            {
                leaderboardUsers.Remove(targetUser);
                continue;
            }

            targetUser.ExportCount = offsetCount;
        }
        
        LeaderboardExports = [..leaderboardExports];
        LeaderboardUsers = [..leaderboardUsers];
            
        var foundUser = LeaderboardUsers.FirstOrDefault(user =>
            user.Identifier == AppSettings.Current.Online.Identification?.Identifier);

        MedalBitmap = foundUser?.MedalBitmap;
    }
}