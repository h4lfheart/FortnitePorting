using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.Leaderboard;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.ViewModels.Leaderboard;


public partial class LeaderboardPersonalViewModel() : LeaderboardViewModelBase<LeaderboardExport>
{
    [ObservableProperty] private SupabaseService _supaBase;

    public LeaderboardPersonalViewModel(SupabaseService supabase) : this()
    {
        SupaBase = supabase;
    }
    
    [ObservableProperty] private LeaderboardStats? _stats;
    [ObservableProperty] private LeaderboardExport _mostPopularExport;
    
    protected override string PageCountFunctionName => "leaderboard_personal_exports_page_count";
    protected override string PageDataFunctionName => "leaderboard_personal_exports";
    

    public override async Task OnViewOpened()
    {
        await base.OnViewOpened();
        
        Stats = await SupaBase.Client.CallObjectFunction<LeaderboardStats>("leaderboard_personal_stats");
        MostPopularExport = new LeaderboardExport
        {
            ObjectPath = Stats.MostPopularObjectPath,
            ExportCount = Stats.MostPopularObjectCount
        };
        
        await MostPopularExport.Load();
    }

    protected override async Task LoadItem(LeaderboardExport item)
    {
        await item.Load();
    }
}