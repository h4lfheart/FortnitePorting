using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Application;
using FortnitePorting.Controls.Assets;
using FortnitePorting.Export;
using FortnitePorting.Models.Assets;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using RestSharp;

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
        var selectedAsset = AssetLoaderCollection.ActiveLoader.SelectedAssets.First();
        var name = selectedAsset.Data.Asset.CreationData.DisplayName;
        var asset = selectedAsset.Data.Asset.CreationData.Object;

        await Exporter.Export(name, asset, EExportType.Outfit, new ExportMetaData
        {
            AssetsRoot = AppSettings.Current.Application.AssetPath,
            Settings = AppSettings.Current.ExportSettings.Blender
        });
    }
}