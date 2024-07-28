using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Controls.Assets;
using FortnitePorting.Export;
using FortnitePorting.Export.Models;
using FortnitePorting.Models.Assets;
using FortnitePorting.Models.Leaderboard;
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
        if (AssetLoaderCollection.ActiveLoader.SelectedAssets.FirstOrDefault(asset => asset.Data.Asset.IsCustom) is
            { } customAsset)
        {
            AppWM.Message("Unsupported Asset", $"{customAsset.Data.Asset.CreationData.DisplayName} cannot be exported.");
            return;
        }
        
        await Exporter.Export(AssetLoaderCollection.ActiveLoader.SelectedAssets, AppSettings.Current.CreateExportMeta());

        if (AppSettings.Current.Online.UseIntegration)
        {
            var exports = AssetLoaderCollection.ActiveLoader.SelectedAssets.Select(asset =>
            {
                var creationData = asset.Data.Asset.CreationData;
                return new PersonalExport(creationData.Object.GetPathName());
            });
            
            await ApiVM.FortnitePorting.PostExportsAsync(exports);
        }
    }
}