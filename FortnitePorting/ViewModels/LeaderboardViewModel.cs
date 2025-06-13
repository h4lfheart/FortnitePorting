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
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Leaderboard;
using FortnitePorting.Models.Supabase.Tables;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.ViewModels.Settings;
using FortnitePorting.Views;
using Microsoft.VisualBasic.Logging;
using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Rendering.RenderActions;
using ScottPlot.TickGenerators;
using ScottPlot.TickGenerators.TimeUnits;
using Supabase.Realtime.PostgresChanges;
using Log = Serilog.Log;

namespace FortnitePorting.ViewModels;

public partial class LeaderboardViewModel : ViewModelBase
{
    [ObservableProperty] private SupabaseService _supaBase;

    public LeaderboardViewModel(SupabaseService supabase)
    {
        SupaBase = supabase;
    }
    
    [ObservableProperty] private ObservableCollection<LeaderboardPersonalExport> _personalExports = [];
    [ObservableProperty] private ObservableCollection<StatisticsModel> _statisticsModels = [];
    [ObservableProperty] private int _currentStreak;

    [ObservableProperty] private Bitmap? _medalBitmap;

    [ObservableProperty] private int _popupValue;

    public async Task UpdateStatic()
    {
        var personalExports = await SupaBase.Client.Rpc<LeaderboardPersonalExport[]>("leaderboard_personal_exports", new { }) ?? [];
        PersonalExports = [..personalExports];

        CurrentStreak = await SupaBase.Client.Rpc<int>("leaderboard_personal_streak", new { });
        
        await TaskService.RunDispatcherAsync(() =>
        {
            StatisticsModels =
            [
                new StatisticsModel("Day", TimeSpan.FromHours(1), 24, PersonalExports),
                new StatisticsModel("Week", TimeSpan.FromDays(1), 7, PersonalExports),
                new StatisticsModel("Month", TimeSpan.FromDays(1), 30, PersonalExports),
                new StatisticsModel("Year", TimeSpan.FromDays(1), DateTime.IsLeapYear(DateTime.Now.Year) ? 366 : 365, PersonalExports),
            ];
        });
        
    }
}