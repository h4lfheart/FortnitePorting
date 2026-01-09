using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;
using FortnitePorting.Models.Leaderboard;
using FortnitePorting.Services;

namespace FortnitePorting.ViewModels.Leaderboard;

public partial class LeaderboardLiveViewModel() : ViewModelBase
{
    [ObservableProperty] private LiveExportService _liveExport;
    
    public LeaderboardLiveViewModel(LiveExportService liveExport) : this()
    {
        LiveExport = liveExport;
    }
}