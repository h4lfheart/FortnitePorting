using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;
using FortnitePorting.Models.Leaderboard;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.ViewModels.Leaderboard;

public partial class LeaderboardStreaksViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<LeaderboardStreak> _streaks = [];

    public override async Task OnViewOpened()
    {
        var streaks = await SupaBase.Client.Rpc<LeaderboardStreak[]>("leaderboard_streaks", new {}) ?? [];
        streaks.ForEach(async streaks => await streaks.Load());
        Streaks = [..streaks];
    }
}