using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;

using FortnitePorting.Export;
using FortnitePorting.Export.Models;
using FortnitePorting.Models.Assets;
using FortnitePorting.Models.Leaderboard;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
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

    [RelayCommand]
    public async Task Export()
    {
        if (AssetLoaderCollection.ActiveLoader.SelectedAssetInfos.FirstOrDefault(info => info.Asset.IsCustom) is
            { } customAsset)
        {
            AppWM.Message("Unsupported Asset", $"{customAsset.Asset.CreationData.DisplayName} cannot be exported.");
            return;
        }
        
        await Exporter.Export(AssetLoaderCollection.ActiveLoader.SelectedAssetInfos, AppSettings.Current.CreateExportMeta(ExportLocation));

        if (AppSettings.Current.Online.UseIntegration)
        {
            var exports = AssetLoaderCollection.ActiveLoader.SelectedAssetInfos.Select(asset =>
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
}