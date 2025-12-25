using System.Threading.Tasks;
using FortnitePorting.Models.Leaderboard;

namespace FortnitePorting.ViewModels.Leaderboard;

public partial class LeaderboardUsersViewModel : LeaderboardViewModelBase<LeaderboardUser>
{
    protected override string PageCountFunctionName => "leaderboard_users_page_count";
    protected override string PageDataFunctionName => "leaderboard_users";

    protected override async Task LoadItem(LeaderboardUser item)
    {
        await item.Load();
    }
}