using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AsyncImageLoader;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData.Binding;
using FortnitePorting.Application;
using FortnitePorting.Controls.Home;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.Framework.Services;
using FortnitePorting.Views;

namespace FortnitePorting.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    public readonly Bitmap DefaultHomeImage = new(Avalonia.Platform.AssetLoader.Open(new Uri("avares://FortnitePorting/Assets/HomeBackground.png")));
    [ObservableProperty] private string loadingText = "Loading Application";
    [ObservableProperty] private FeaturedArtItem? currentFeaturedArt;
    [ObservableProperty] private int currentFeaturedArtIndex;
    [ObservableProperty] private ObservableCollection<FeaturedArtItem> featuredArt = new();
    [ObservableProperty] private ObservableCollection<ChangelogItem> changelogs = new();
    [ObservableProperty] private DispatcherTimer featuredArtTimer = new();
    [ObservableProperty] private Bitmap splashArtSource;

    public override async Task Initialize()
    {
        SplashArtSource = AppSettings.Current.UseCustomSplashArt ? new Bitmap(AppSettings.Current.CustomSplashArtPath) : DefaultHomeImage;
        TaskService.Run(async () =>
        {
            var changelogEntries = await EndpointsVM.FortnitePorting.GetChangelogsAsync();
            if (changelogEntries is null) return;

            await TaskService.RunDispatcherAsync(() =>
            {
                foreach (var entry in changelogEntries.OrderBy(x => x.PublishDate, SortExpressionComparer<DateTime>.Descending(x => x))) Changelogs.Add(new ChangelogItem(entry));
            });
        });

        TaskService.Run(async () =>
        {
            var featured = await EndpointsVM.FortnitePorting.GetFeaturedAsync();
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
        Launch(Globals.DISCORD_URL);
    }

    [RelayCommand]
    public void OpenFAQ()
    {
        // TODO MAKE FAQ
    }
}