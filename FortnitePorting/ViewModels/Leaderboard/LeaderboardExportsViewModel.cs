using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;
using FortnitePorting.Models.Leaderboard;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.ViewModels.Leaderboard;

public partial class LeaderboardExportsViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<LeaderboardExport> _exports = [];

    public override async Task OnViewOpened()
    {
        var exports = await SupaBase.Client.Rpc<LeaderboardExport[]>("leaderboard_exports", new {}) ?? [];
        exports.ForEach(async export => await export.Load());
        Exports = [..exports];
    }
}