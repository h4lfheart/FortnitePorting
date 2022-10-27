using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.UObject;
using FortnitePorting.AppUtils;
using FortnitePorting.Views.Controls;
using FortnitePorting.Views.Extensions;
using Serilog;

namespace FortnitePorting.ViewModels;

public class AssetHandlerViewModel
{
    public readonly Dictionary<EAssetType, AssetHandlerData> Handlers ;
    
    public AssetHandlerViewModel()
    {
        Handlers = new Dictionary<EAssetType, AssetHandlerData>
        {
            { EAssetType.Outfit, OutfitHandler },
            { EAssetType.Backpack, BackpackHandler },
            { EAssetType.Pickaxe, PickaxeHandler },
            { EAssetType.Glider, GliderHandler },
            { EAssetType.Weapon, WeaponHandler },
            { EAssetType.Dance, DanceHandler },
        };

    }

    private readonly AssetHandlerData OutfitHandler = new()
    {
        AssetType = EAssetType.Outfit,
        TargetCollection = AppVM.MainVM.Outfits,
        ClassNames = new List<string> { "AthenaCharacterItemDefinition" },
        RemoveList = new List<string> { "_NPC", "_TBD", "_VIP", "_Creative", "_SG"},
        IconGetter = asset =>
        {
            asset.TryGetValue(out UTexture2D? previewImage, "SmallPreviewImage", "LargePreviewImage");
            if (asset.TryGetValue(out UObject heroDef, "HeroDefinition"))
            {
                heroDef.TryGetValue(out previewImage, "SmallPreviewImage", "LargePreviewImage");
            }

            return previewImage;
        }
    };
    
    private readonly AssetHandlerData BackpackHandler = new()
    {
        AssetType = EAssetType.Backpack,
        TargetCollection = AppVM.MainVM.BackBlings,
        ClassNames = new List<string> { "AthenaBackpackItemDefinition" },
        RemoveList = new List<string> { "_STWHeroNoDefaultBackpack", "_TEST", "Dev_", "_NPC", "_TBD"},
        IconGetter = asset => asset.GetOrDefault<UTexture2D?>("SmallPreviewImage", "LargePreviewImage")
    };
    
    private readonly AssetHandlerData PickaxeHandler = new()
    {
        AssetType = EAssetType.Pickaxe,
        TargetCollection = AppVM.MainVM.HarvestingTools,
        ClassNames = new List<string> { "AthenaPickaxeItemDefinition" },
        RemoveList = new List<string> { "Dev_", "TBD_" },
        IconGetter = asset =>
        {
            asset.TryGetValue(out UTexture2D? previewImage, "SmallPreviewImage", "LargePreviewImage");
            if (asset.TryGetValue(out UObject heroDef, "WeaponDefinition"))
            {
                heroDef.TryGetValue(out previewImage, "SmallPreviewImage", "LargePreviewImage");
            }
            return previewImage;
        }
    };
    
    private readonly AssetHandlerData GliderHandler = new()
    {
        AssetType = EAssetType.Glider,
        TargetCollection = AppVM.MainVM.Gliders,
        ClassNames = new List<string> { "AthenaGliderItemDefinition" },
        RemoveList = { },
        IconGetter = asset => asset.GetOrDefault<UTexture2D?>("SmallPreviewImage", "LargePreviewImage")
    };
    
    private readonly AssetHandlerData WeaponHandler = new()
    {
        AssetType = EAssetType.Weapon,
        TargetCollection = AppVM.MainVM.Weapons,
        ClassNames = new List<string> { "FortWeaponRangedItemDefinition" },
        RemoveList = {},
        IconGetter = asset => asset.GetOrDefault<UTexture2D?>("SmallPreviewImage", "LargePreviewImage")
    };
    
    private readonly AssetHandlerData DanceHandler = new()
    {
        AssetType = EAssetType.Dance,
        TargetCollection = AppVM.MainVM.Dances,
        ClassNames = new List<string> { "AthenaDanceItemDefinition" },
        RemoveList = { "_CT", "_NPC"},
        IconGetter = asset => asset.GetOrDefault<UTexture2D?>("SmallPreviewImage", "LargePreviewImage")
    };

    public async Task Initialize()
    {
        await OutfitHandler.Execute(); // default tab
    }
}


public class AssetHandlerData
{
    public bool HasStarted { get; private set; }
    public Pauser PauseState { get; } = new();
    
    public EAssetType AssetType;
    public ObservableCollection<AssetSelectorItem> TargetCollection;
    public List<string> ClassNames;
    public List<string> RemoveList = Enumerable.Empty<string>().ToList();
    public Func<UObject, UTexture2D?> IconGetter;

    public async Task Execute()
    {
        if (HasStarted) return;
        HasStarted = true;

        var items = AppVM.CUE4ParseVM.AssetDataBuffers
            .Where(x => ClassNames.Any(y => x.AssetClass.PlainText.Equals(y, StringComparison.OrdinalIgnoreCase)))
            .Where(x => !RemoveList.Any(y => x.AssetName.PlainText.Contains(y, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        
        // prioritize random first cuz of parallel list positions
        var random = items.FirstOrDefault(x => x.AssetName.PlainText.Contains("Random", StringComparison.OrdinalIgnoreCase));
        if (random is not null)
        {
            items.Remove(random);
            await DoLoad(random, true);
        }

        var addedAssets = new List<string>();

        await Parallel.ForEachAsync(items, async (data, token) =>
        {
            var assetName = data.AssetName.PlainText;
            if (AssetType == EAssetType.Weapon)
            {
                var reg = Regex.Match(assetName, @"(.*)_(.*)_(.*)_T[0-9][0-9]");
                if (reg.Success && addedAssets.Any(x => x.Contains(reg.Groups[1].Value, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }
                addedAssets.Add(assetName);
            }
            
            await DoLoad(data);
        });
    }

    private async Task DoLoad(FAssetData data, bool random = false)
    {
        await PauseState.WaitIfPaused();
        var asset = await AppVM.CUE4ParseVM.Provider.LoadObjectAsync(data.ObjectPath);

        var previewImage = IconGetter(asset);
        if (previewImage is null) return;
            
        await Application.Current.Dispatcher.InvokeAsync(() => TargetCollection.Add(new AssetSelectorItem(asset, previewImage, random)), DispatcherPriority.Background);
    }
}