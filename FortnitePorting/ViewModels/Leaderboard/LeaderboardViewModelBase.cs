using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.ViewModels.Leaderboard;

public abstract partial class LeaderboardViewModelBase<T> : ViewModelBase where T : class
{
    [ObservableProperty] private ObservableCollection<T> _items = [];
    
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private string _pageInfo = "Page 1 of 1";

    protected abstract string PageCountFunctionName { get; }
    protected abstract string PageDataFunctionName { get; }
    
    public override async Task OnViewOpened()
    {
        TotalPages = await SupaBase.Client.CallPrimitiveFunction<int>(PageCountFunctionName);
        await LoadPage(1);
    }

    protected virtual async Task LoadPage(int page)
    {
        var items = await SupaBase.Client.CallTableFunction<T>(PageDataFunctionName, new { page });
        
        items.ForEach(async item => await LoadItem(item));
        Items = [..items];
        
        CurrentPage = page;
        UpdatePageInfo();
    }

    protected virtual async Task LoadItem(T item)
    {
        
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

    private bool CanGoPrevious() => CurrentPage > 1;

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private async Task NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            await LoadPage(CurrentPage + 1);
        }
    }

    private bool CanGoNext() => CurrentPage < TotalPages;
}