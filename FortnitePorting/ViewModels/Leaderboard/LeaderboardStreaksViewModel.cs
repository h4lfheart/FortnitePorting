using System.Threading.Tasks;
using FortnitePorting.Models.Leaderboard;

namespace FortnitePorting.ViewModels.Leaderboard;

public partial class LeaderboardStreaksViewModel : LeaderboardViewModelBase<LeaderboardStreak>
{
    protected override string PageCountFunctionName => "leaderboard_streaks_page_count";
    protected override string PageDataFunctionName => "leaderboard_streaks";

    protected override async Task LoadItem(LeaderboardStreak item)
    {
        await item.Load();
    }
}