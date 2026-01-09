using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Controls.Navigation.Sidebar;
using FortnitePorting.Exporting;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.Assets.Asset;
using FortnitePorting.Models.Assets.Custom;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Views;
using Material.Icons;

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
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowNamesIcon))] private bool _showNames = AppSettings.Application.ShowAssetNames;

    public MaterialIconKind ShowNamesIcon => ShowNames ? MaterialIconKind.TextLong : MaterialIconKind.TextShort;
    
    [ObservableProperty] private ObservableCollection<ISidebarItem> _sidebarItems = [];
    
    public override async Task Initialize()
    {
        await TaskService.RunDispatcherAsync(() =>
        {
            foreach (var (index, category) in AssetLoader.Categories.Enumerate())
            {
                SidebarItems.Add(new SidebarItemText(category.Category.Description.ToUpper()));

                foreach (var loader in category.Loaders)
                {
                    SidebarItems.Add(new SidebarItemButton(
                        text: loader.Type.Description, 
                        iconBitmap: ImageExtensions.AvaresBitmap($"avares://FortnitePorting/Assets/FN/{loader.Type.ToString()}.png"),
                        tag: loader.Type
                    ));
                }
                
                if (index < AssetLoader.Categories.Count - 1)
                    SidebarItems.Add(new SidebarItemSeparator());
            }
        });
    }

    public override async Task OnViewExited()
    {
        AppSettings.Application.ShowAssetNames = ShowNames;
    }

    [RelayCommand]
    public async Task SetExportLocation(EExportLocation location)
    {
        ExportLocation = location;
    }

    [RelayCommand]
    public async Task Export()
    {
        if (AssetLoader.ActiveLoader is null) return;
        
        AssetLoader.ActiveLoader.Pause();
        
        var exportedProperly = await Exporter.Export(AssetLoader.ActiveLoader.SelectedAssetInfos, AppSettings.ExportSettings.CreateExportMeta(ExportLocation));
        if (exportedProperly && SupaBase.IsLoggedIn)
        {
            await SupaBase.PostExports([
                ..AssetLoader.ActiveLoader.SelectedAssetInfos
                    .OfType<AssetInfo>()
                    .Select(asset => asset.Asset.CreationData.Object.GetPathName()),
                ..AssetLoader.ActiveLoader.SelectedAssetInfos
                    .OfType<CustomAssetInfo>()
                    .Select(asset => $"Custom/{asset.Asset.Asset.Name}"),
            ]);
        }
        
        AssetLoader.ActiveLoader.Unpause();
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