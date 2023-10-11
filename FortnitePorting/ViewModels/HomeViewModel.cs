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
using FortnitePorting.Services.Endpoints.Models;

namespace FortnitePorting.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    [ObservableProperty] private string loadingText = "Loading Application";
    [ObservableProperty] private string featuredArtist = "None";
    [ObservableProperty] private string featuredArtURL = string.Empty;
    [ObservableProperty] private ObservableCollection<ChangelogItem> changelogs = new();
    
    public override async Task Initialize()
    {
        await TaskService.RunAsync(async () =>
        {
            var changelogEntries = await EndpointService.FortnitePorting.GetChangelogsAsync();
            if (changelogEntries is not null)
            {
                await TaskService.RunDispatcherAsync(() =>
                {
                    foreach (var entry in changelogEntries.OrderBy(x => x.PublishDate, SortExpressionComparer<DateTime>.Descending(x => x)))
                    {
                        Changelogs.Add(new ChangelogItem(entry));
                    }
                });
            }
        });
        
        await TaskService.RunAsync(async () =>
        {
            var featured = await EndpointService.FortnitePorting.GetFeaturedAsync();
            if (featured is not null)
            {
                FeaturedArtist = featured.Artist;
                FeaturedArtURL = featured.ImageURL;
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