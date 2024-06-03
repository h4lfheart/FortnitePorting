using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Controls.Assets;
using FortnitePorting.Models.API;
using FortnitePorting.Models.Assets;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;
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
        await AssetLoaderCollection.Load(EAssetType.Outfit);
    }

    [RelayCommand]
    public async Task Export()
    {
        var asset = AssetLoaderCollection.ActiveLoader.SelectedAssets.First();
        var name = asset.Data.Asset.CreationData.DisplayName;

        await ApiVM.FortnitePortingServer.SendAsync(name, EExportServerType.Blender);
    }
}