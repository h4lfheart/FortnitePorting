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

public partial class LeaderboardPersonalViewModel() : ViewModelBase
{
    [ObservableProperty] private SupabaseService _supaBase;

    public LeaderboardPersonalViewModel(SupabaseService supabase) : this()
    {
        SupaBase = supabase;
    }
    
    [ObservableProperty] private ObservableCollection<LeaderboardExport> _exports = [];
    [ObservableProperty] private LeaderboardStats? _stats;
    [ObservableProperty] private LeaderboardExport _mostPopularExport;
    
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private string _pageInfo = "Page 1 of 1";

    public bool IsLoading
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                PreviousPageCommand.NotifyCanExecuteChanged();
                NextPageCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public override async Task OnViewOpened()
    {
        Stats = await SupaBase.Client.CallObjectFunction<LeaderboardStats>("leaderboard_personal_stats");
        MostPopularExport = new LeaderboardExport
        {
            ObjectPath = Stats.MostPopularObjectPath,
            ExportCount = Stats.MostPopularObjectCount
        };
        await MostPopularExport.Load();
        
        TotalPages = await SupaBase.Client.CallPrimitiveFunction<int>("leaderboard_personal_exports_page_count");
        
        await LoadPage(1);
    }

    private async Task LoadPage(int page)
    {
        IsLoading = true;
        
        var exports = await SupaBase.Client.CallTableFunction<LeaderboardExport>("leaderboard_personal_exports", new
        {
            page
        });
        
        exports.ForEach(async export => await export.Load());
        Exports = [..exports];
        
        CurrentPage = page;
        UpdatePageInfo();
        
        IsLoading = false;
    }

    private void UpdatePageInfo()
    {
        PageInfo = $"Page {CurrentPage} of {TotalPages}";
        
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private async Task PreviousPage()
    {
        if (CurrentPage > 1)
        {
            await LoadPage(CurrentPage - 1);
        }
    }

    private bool CanGoPrevious() => CurrentPage > 1 && !IsLoading;

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private async Task NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            await LoadPage(CurrentPage + 1);
        }
    }

    private bool CanGoNext() => CurrentPage < TotalPages && !IsLoading;
}