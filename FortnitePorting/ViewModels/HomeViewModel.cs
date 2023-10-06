using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData.Binding;
using FortnitePorting.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Services;

namespace FortnitePorting.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    [ObservableProperty] private string loadingText = "Loading Application";
    [ObservableProperty] private ObservableCollection<ChangelogItem> changelogs = new();
    
    public override async Task Initialize()
    {
        var changelogEntries = await EndpointService.FortnitePorting.GetChangelogsAsync();
        if (changelogEntries is null) return;
        
        await TaskService.RunDispatcherAsync(() =>
        {
            foreach (var entry in changelogEntries.OrderBy(x => x.PublishDate, SortExpressionComparer<DateTime>.Descending(x => x)))
            {
                Changelogs.Add(new ChangelogItem(entry));
            }
        });
    }
    
    public void Update(string text)
    {
        LoadingText = text;
    }
    
    [RelayCommand]
    public void OpenDiscord()
    {
        AppVM.Launch(Globals.DISCORD_URL);
    }
    
    [RelayCommand]
    public void OpenFAQ()
    {
        // TODO MAKE FAQ
    }
}