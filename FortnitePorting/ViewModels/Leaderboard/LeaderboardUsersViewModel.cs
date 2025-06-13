using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;
using FortnitePorting.Models.Leaderboard;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.ViewModels.Leaderboard;

public partial class LeaderboardUsersViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<LeaderboardUser> _users = [];
    
    public override async Task OnViewOpened()
    {
        var users = await SupaBase.Client.Rpc<LeaderboardUser[]>("leaderboard_users", new {}) ?? [];
        users.ForEach(async user => await user.Load());
        Users = [..users];
    }
}