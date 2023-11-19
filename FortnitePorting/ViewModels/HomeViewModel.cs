using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData.Binding;
using FortnitePorting.Controls.Home;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.Views;

namespace FortnitePorting.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    [ObservableProperty] private string loadingText = "Loading Application";
    [ObservableProperty] private FeaturedArtItem? currentFeaturedArt;
    [ObservableProperty] private int currentFeaturedArtIndex;
    [ObservableProperty] private ObservableCollection<FeaturedArtItem> featuredArt = new();
    [ObservableProperty] private ObservableCollection<ChangelogItem> changelogs = new();
    [ObservableProperty] private DispatcherTimer featuredArtTimer = new();

    public override async Task Initialize()
    {
        TaskService.Run(async () =>
        {
            var changelogEntries = await EndpointService.FortnitePorting.GetChangelogsAsync();
            if (changelogEntries is null) return;

            await TaskService.RunDispatcherAsync(() =>
            {
                foreach (var entry in changelogEntries.OrderBy(x => x.PublishDate, SortExpressionComparer<DateTime>.Descending(x => x))) Changelogs.Add(new ChangelogItem(entry));
            });
        });

        TaskService.Run(async () =>
        {
            var featured = await EndpointService.FortnitePorting.GetFeaturedAsync();
            if (featured is null) return;

            await TaskService.RunDispatcherAsync(() =>
            {
                foreach (var feature in featured) FeaturedArt.Add(new FeaturedArtItem(feature));
            });

            CurrentFeaturedArt = FeaturedArt.FirstOrDefault();
            if (FeaturedArt.Count > 1) DispatcherTimer.Run(ChangeFeaturedArt, TimeSpan.FromSeconds(5));
        });
    }

    public bool ChangeFeaturedArt()
    {
        if (MainVM.ActiveTab is not HomeView) return true;

        if (CurrentFeaturedArtIndex >= FeaturedArt.Count) CurrentFeaturedArtIndex = 0;

        CurrentFeaturedArt = FeaturedArt[CurrentFeaturedArtIndex];
        CurrentFeaturedArtIndex++;

        return true;
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