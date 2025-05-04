using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Exporting;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.API;
using FortnitePorting.Models.Assets;
using FortnitePorting.Models.Assets.Asset;
using FortnitePorting.Models.Leaderboard;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Services;
using FortnitePorting.Views;
using RestSharp;

namespace FortnitePorting.ViewModels;

public partial class AssetsViewModel() : ViewModelBase
{
    [ObservableProperty] private AssetLoaderService _assetLoader;
    [ObservableProperty] private APIService _api;
    [ObservableProperty] private SupabaseService _supabase;

    public AssetsViewModel(AssetLoaderService assetLoader, APIService api, SupabaseService supabase) : this()
    {
        AssetLoader = assetLoader;
        Api = api;
        Supabase = supabase;
    }
    
    [ObservableProperty] private bool _isPaneOpen = true;
    [ObservableProperty] private EExportLocation _exportLocation = EExportLocation.Blender;
    
    [ObservableProperty] private ObservableCollection<NavigationViewItem> _navItems = [];
    [ObservableProperty] private NavigationViewItem _selectedNavItem;
    
    public override async Task Initialize()
    {
        await TaskService.RunDispatcherAsync(() =>
        {
            foreach (var category in AssetLoader.Categories)
            {
                NavItems.Add(new NavigationViewItem
                {
                    Tag = category.Category,
                    Content = category.Category.GetDescription(),
                    SelectsOnInvoked = false,
                    IconSource = new ImageIconSource
                    {
                        Source = ImageExtensions.AvaresBitmap($"avares://FortnitePorting/Assets/FN/{category.Category.ToString()}.png")
                    },
                    MenuItemsSource = category.Loaders.Select(loader => new NavigationViewItem
                    {
                        Tag = loader.Type, 
                        Content = loader.Type.GetDescription(), 
                        IconSource = new ImageIconSource
                        {
                            Source = ImageExtensions.AvaresBitmap($"avares://FortnitePorting/Assets/FN/{loader.Type.ToString()}.png")
                        },
                    })
                });
            }
        });
    }

    public override async Task OnViewOpened()
    {
        await AssetLoader.Load(EExportType.Outfit);
    }

    [RelayCommand]
    public async Task Export()
    {
        AssetLoader.ActiveLoader.Pause();
        await Exporter.Export(AssetLoader.ActiveLoader.SelectedAssetInfos, AppSettings.ExportSettings.CreateExportMeta(ExportLocation));
        AssetLoader.ActiveLoader.Unpause();

        if (SupaBase.IsActive)
        {
            await SupaBase.PostExports(
                AssetLoader.ActiveLoader.SelectedAssetInfos
                    .OfType<AssetInfo>()
                    .Select(asset => asset.Asset.CreationData.Object.GetPathName())
            );
        }
    }
    
    [RelayCommand]
    public async Task Favorite()
    {
        foreach (var info in AssetLoader.ActiveLoader.SelectedAssetInfos)
        {
            info.Asset.Favorite();
        }
    }

    [RelayCommand]
    public async Task OpenSettings()
    {
        Navigation.App.Open<ExportSettingsView>();
        Navigation.ExportSettings.Open(ExportLocation);
    }
}