using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;
using FortnitePorting.Models.Leaderboard;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.ViewModels.Leaderboard;

public partial class LeaderboardExportsViewModel : PagedLeaderboardViewModelBase<LeaderboardExport>
{
    protected override string PageCountFunctionName => "leaderboard_exports_page_count";
    protected override string PageDataFunctionName => "leaderboard_exports";

    protected override async Task LoadItem(LeaderboardExport item)
    {
        await item.Load();
    }
}