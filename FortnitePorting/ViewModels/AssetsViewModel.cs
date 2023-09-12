using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using FortnitePorting.Controls;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using Serilog;

namespace FortnitePorting.ViewModels;

public partial class AssetsViewModel : ViewModelBase
{
    [ObservableProperty] private EExportType exportType = EExportType.Blender;

    [ObservableProperty] private ObservableCollection<AssetItem> outfits = new();

    private readonly AssetLoader OutfitLoader;

    public AssetsViewModel()
    {
        OutfitLoader = new AssetLoader(EAssetType.Outfit)
        {
            Target = Outfits,
            Classes = new[] { "AthenaCharacterItemDefinition" },
            IconHandler = asset =>
            {
                asset.TryGetValue(out UTexture2D? previewImage, "SmallPreviewImage", "LargePreviewImage");
                if (asset.TryGetValue(out UObject heroDef, "HeroDefinition"))
                {
                    heroDef.TryGetValue(out previewImage, "SmallPreviewImage", "LargePreviewImage");
                }

                return previewImage;
            }
        };
    }

    public override async Task Initialize()
    {
        await OutfitLoader.Load();
    }
}

public class AssetLoader
{
    public bool Started;
    
    public ObservableCollection<AssetItem> Target;
    public string[] Classes = Array.Empty<string>();
    public Func<UObject, UTexture2D?> IconHandler = asset => asset.GetAnyOrDefault<UTexture2D?>("SmallPreviewImage", "LargePreviewImage");
    public EAssetType Type;
    
    public AssetLoader(EAssetType type)
    {
        Type = type;
    }
    
    public async Task Load()
    {
        if (Started) return;
        Started = true;

        var assets = CUE4ParseVM.AssetRegistry.Where(data => Classes.Any(className => data.AssetClass.Text.Equals(className, StringComparison.OrdinalIgnoreCase)));

        await Parallel.ForEachAsync(assets, async (data, _) =>
        {
            await LoadAsset(data);
        });
    }

    private async Task LoadAsset(FAssetData data)
    {
        var asset = await CUE4ParseVM.Provider.LoadObjectAsync(data.ObjectPath);
        var icon = IconHandler(asset);
        if (icon is null) return;

        await Dispatcher.UIThread.InvokeAsync(() => Target.Add(new AssetItem(asset, icon)), DispatcherPriority.Background);
    }
}