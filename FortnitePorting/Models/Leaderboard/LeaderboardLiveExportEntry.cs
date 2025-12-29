using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Application;
using FortnitePorting.Models.Chat;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Models.Leaderboard;

public partial class LeaderboardLiveExportEntry : ObservableObject
{
    [ObservableProperty] private string _instanceId;
    [ObservableProperty] private string _userId;
    [ObservableProperty] private ChatUser _user;
    [ObservableProperty] private DateTime _timestamp;
    [ObservableProperty] private ObservableCollection<LeaderboardLiveExport> _exports = [];

    public LeaderboardLiveExportEntry(string instanceId, string userId, DateTime timestamp, LeaderboardLiveExport[] exports)
    {
        InstanceId = instanceId;
        UserId = userId;
        Timestamp = timestamp;
        Exports = [..exports];
        
        TaskService.Run(Load);
    }

    public async Task Load()
    {
        User = await AppServices.Chat.GetUser(UserId);
        foreach (var export in Exports)
        {
            await export.Load();
        }
    }
    
}