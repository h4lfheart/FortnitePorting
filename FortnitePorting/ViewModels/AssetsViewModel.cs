using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;

using FortnitePorting.Export;
using FortnitePorting.Export.Models;
using FortnitePorting.Framework;
using FortnitePorting.Models.Assets;
using FortnitePorting.Models.Assets.Asset;
using FortnitePorting.Models.Leaderboard;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Services;
using FortnitePorting.Views;
using RestSharp;
using AssetLoaderCollection = FortnitePorting.Models.Assets.Loading.AssetLoaderCollection;

namespace FortnitePorting.ViewModels;

public partial class AssetsViewModel : ViewModelBase
{
    [ObservableProperty] private AssetLoaderCollection _assetLoaderCollection;
    
    [ObservableProperty] private bool _isPaneOpen = true;
    [ObservableProperty] private EExportLocation _exportLocation = EExportLocation.Blender;
    
    public override async Task Initialize()
    {
        AssetLoaderCollection = new AssetLoaderCollection();
        await AssetLoaderCollection.Load(EExportType.Outfit);
    }

    public override async Task OnViewOpened()
    {
        DiscordService.Update(AssetLoaderCollection?.ActiveLoader?.Type ?? EExportType.Outfit);
    }

    [RelayCommand]
    public async Task Export()
    {
        AssetLoaderCollection.ActiveLoader.Pause();
        await Exporter.Export(AssetLoaderCollection.ActiveLoader.SelectedAssetInfos, AppSettings.Current.CreateExportMeta(ExportLocation));
        AssetLoaderCollection.ActiveLoader.Unpause();

        if (AppSettings.Current.Online.UseIntegration)
        {
            var exports = AssetLoaderCollection.ActiveLoader.SelectedAssetInfos.OfType<AssetInfo>().Select(asset =>
            {
                var creationData = asset.Asset.CreationData;
                return new PersonalExport(creationData.Object.GetPathName());
            });
            
            await ApiVM.FortnitePorting.PostExportsAsync(exports);
        }
    }
    
    [RelayCommand]
    public async Task Favorite()
    {
        foreach (var info in AssetLoaderCollection.ActiveLoader.SelectedAssetInfos)
        {
            info.Asset.Favorite();
        }
    }

    [RelayCommand]
    public async Task OpenSettings()
    {
        AppWM.Navigate<ExportSettingsView>();
        AppSettings.Current.ExportSettings.Navigate(ExportLocation);
    }
}