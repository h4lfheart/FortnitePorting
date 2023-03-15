using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FortnitePorting.AppUtils;
using FortnitePorting.Views.Controls;

namespace FortnitePorting.ViewModels;

public partial class WrappedViewModel : ObservableObject
{
    [ObservableProperty] private AssetSelectorItem asset;
    [ObservableProperty] private string assetDescription;
    [ObservableProperty] private AssetSelectorItem music;
    [ObservableProperty] private string musicDescription;
    [ObservableProperty] private string timeString;
    
    public async Task Initialize()
    {
        await Task.Run(async () =>
        {
            // Asset
            if (AppSettings.Current.WrappedData.ItemsExported.Count > 0)
            {
                var wrappedData = AppSettings.Current.WrappedData.ItemsExported.MaxBy(x => x.Value.Count);
                var loadedObject = await AppVM.CUE4ParseVM.Provider.LoadObjectAsync(wrappedData.Key);
                var previewImage = AppVM.AssetHandlerVM?.Handlers[wrappedData.Value.Type].IconGetter(loadedObject);
                previewImage ??= AppVM.CUE4ParseVM.PlaceholderTexture;
            
                await Application.Current.Dispatcher.InvokeAsync(() => 
                { 
                     Asset = new AssetSelectorItem(loadedObject, previewImage, EAssetType.Invalid);
                     AssetDescription = $"Your most exported item was \"{Asset.DisplayName}\".\nYou exported this item {wrappedData.Value.Count} time(s)!";
                });
            }

            // Music
            if (AppSettings.Current.WrappedData.MusicPlayed.Count > 0)
            {
                var wrappedData = AppSettings.Current.WrappedData.MusicPlayed.MaxBy(x => x.Value.Count);
                var loadedObject = await AppVM.CUE4ParseVM.Provider.LoadObjectAsync(wrappedData.Key);
                var previewImage = AppVM.AssetHandlerVM?.Handlers[wrappedData.Value.Type].IconGetter(loadedObject);
                previewImage ??= AppVM.CUE4ParseVM.PlaceholderTexture;
            
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Music = new AssetSelectorItem(loadedObject, previewImage, EAssetType.Invalid);
                    MusicDescription = $"Your favorite music pack to listen to was \"{Music.DisplayName}\".\nYou listened this music pack {wrappedData.Value.Count} time(s)!";
                });
            }
        });
    }
}