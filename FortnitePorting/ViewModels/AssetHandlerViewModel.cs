using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.AppUtils;
using FortnitePorting.Exports;
using FortnitePorting.Views.Controls;
using FortnitePorting.Views.Extensions;

namespace FortnitePorting.ViewModels;

public class AssetHandlerViewModel
{
    public readonly Dictionary<EAssetType, AssetHandlerData> Handlers;

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
            { EAssetType.Vehicle, VehicleHandler },
            { EAssetType.Prop, PropHandler },
            { EAssetType.Pet, PetHandler },
            { EAssetType.Music, MusicPackHandler }
        };
    }

    private readonly AssetHandlerData OutfitHandler = new()
    {
        AssetType = EAssetType.Outfit,
        TargetCollection = AppVM.MainVM.Outfits,
        ClassNames = new List<string> { "AthenaCharacterItemDefinition" },
        RemoveList = new List<string> { "_NPC", "_TBD", "CID_VIP", "_Creative", "_SG" },
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
        RemoveList = new List<string> { "_STWHeroNoDefaultBackpack", "_TEST", "Dev_", "_NPC", "_TBD" },
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
        ClassNames = new List<string> { "FortWeaponRangedItemDefinition", "FortWeaponMeleeItemDefinition", "FortCreativeWeaponMeleeItemDefinition", "FortCreativeWeaponRangedItemDefinition" },
        RemoveList = { "_Harvest", "Weapon_Pickaxe_", "Weapons_Pickaxe_", "Dev_WID" },
        IconGetter = asset => asset.GetOrDefault<UTexture2D?>("SmallPreviewImage", "LargePreviewImage")
    };

    private readonly AssetHandlerData DanceHandler = new()
    {
        AssetType = EAssetType.Dance,
        TargetCollection = AppVM.MainVM.Dances,
        ClassNames = new List<string> { "AthenaDanceItemDefinition" },
        RemoveList = { "_CT", "_NPC" },
        IconGetter = asset => asset.GetOrDefault<UTexture2D?>("SmallPreviewImage", "LargePreviewImage")
    };

    private readonly AssetHandlerData VehicleHandler = new()
    {
        AssetType = EAssetType.Vehicle,
        TargetCollection = AppVM.MainVM.Vehicles,
        ClassNames = new List<string> { "FortVehicleItemDefinition" },
        RemoveList = { },
        IconGetter = asset =>
        {
            var icon = asset.GetOrDefault<UTexture2D?>("SmallPreviewImage", "LargePreviewImage");
            if (icon is null)
            {
                var blueprint = asset.Get<UBlueprintGeneratedClass>("VehicleActorClass");
                var classDefaultObject = blueprint.ClassDefaultObject.Load();
                var markerDisplay = classDefaultObject?.GetOrDefault<FStructFallback>("MarkerDisplay");
                icon = markerDisplay?.GetOrDefault<UTexture2D?>("Icon");
                if (icon is null)
                {
                    var superStruct = blueprint.SuperStruct.Load<UBlueprintGeneratedClass>();
                    var superClassDefaultObject = superStruct.ClassDefaultObject.Load();
                    var markerDisplaySuper = superClassDefaultObject?.Get<FStructFallback>("MarkerDisplay");
                    icon = markerDisplaySuper?.GetOrDefault<UTexture2D?>("Icon");
                }
            }

            return icon;
        },
        DisplayNameGetter = asset =>
        {
            var displayText = asset.GetOrDefault<FText?>("DisplayName");
            if (displayText is null)
            {
                var blueprint = asset.Get<UBlueprintGeneratedClass>("VehicleActorClass");
                var classDefaultObject = blueprint.ClassDefaultObject.Load();
                var markerDisplay = classDefaultObject?.GetOrDefault<FStructFallback>("MarkerDisplay");
                displayText = markerDisplay?.GetOrDefault<FText?>("DisplayName");
                if (displayText is null)
                {
                    var configClass = classDefaultObject?.GetOrDefault<UBlueprintGeneratedClass?>("VehicleConfigsClass");
                    var configClassDefaultObject = configClass?.ClassDefaultObject.Load();
                    displayText = configClassDefaultObject?.GetOrDefault<FText?>("PlayerFacingLocName");
                }

                if (displayText is null)
                {
                    var superStruct = blueprint.SuperStruct.Load<UBlueprintGeneratedClass>();
                    var superClassDefaultObject = superStruct.ClassDefaultObject.Load();
                    var markerDisplaySuper = superClassDefaultObject?.Get<FStructFallback>("MarkerDisplay");
                    displayText = markerDisplaySuper?.GetOrDefault<FText?>("DisplayName");
                }
            }

            return displayText;
        }
    };

    private readonly AssetHandlerData PropHandler = new()
    {
        AssetType = EAssetType.Prop,
        TargetCollection = AppVM.MainVM.Props,
        ClassNames = new List<string> { "FortPlaysetPropItemDefinition" },
        RemoveList = { },
        IconGetter = asset => asset.GetOrDefault<UTexture2D?>("SmallPreviewImage", "LargePreviewImage")
    };

    private readonly AssetHandlerData PetHandler = new()
    {
        AssetType = EAssetType.Pet,
        TargetCollection = AppVM.MainVM.Pets,
        ClassNames = new List<string> { "AthenaPetCarrierItemDefinition" },
        RemoveList = { },
        IconGetter = asset => asset.GetOrDefault<UTexture2D?>("SmallPreviewImage", "LargePreviewImage")
    };

    private readonly AssetHandlerData MusicPackHandler = new()
    {
        AssetType = EAssetType.Music,
        TargetCollection = AppVM.MainVM.MusicPacks,
        ClassNames = new List<string> { "AthenaMusicPackItemDefinition" },
        RemoveList = { },
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
    public ObservableCollection<AssetSelectorItem>? TargetCollection;
    public List<string> ClassNames;
    public List<string> RemoveList = Enumerable.Empty<string>().ToList();
    public Func<UObject, UTexture2D?> IconGetter;
    public Func<UObject, FText?>? DisplayNameGetter;

    public async Task Execute()
    {
        if (HasStarted) return;
        HasStarted = true;
        var sw = new Stopwatch();
        sw.Start();

        if (AssetType is EAssetType.Weapon && AppSettings.Current.WeaponMappings.Count == 0)
        {
            AppLog.Warning("Generating first-time weapon mappings, this may take longer than usual");
        }

        var propDictionary = new Dictionary<string, string>(); // prop asset name : gallery
        if (AssetType is EAssetType.Prop)
        {
            var playsets = AppVM.CUE4ParseVM.AssetDataBuffers.Where(x => x.AssetClass.Text.Equals("FortPlaysetItemDefinition", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var playset in playsets)
            {
                var playsetObject = await AppVM.CUE4ParseVM.Provider.LoadObjectAsync(playset.ObjectPath);
                var creativeTagsHelper = playsetObject.GetOrDefault("CreativeTagsHelper", new FStructFallback());
                var creativeTags = creativeTagsHelper.GetOrDefault("CreativeTags", Array.Empty<FName>());
                if (!creativeTags.Any(x => x.Text.Contains("Gallery", StringComparison.OrdinalIgnoreCase))) continue;

                var playsetName = playsetObject.GetOrDefault("DisplayName", new FText("Unknown Gallery"));
                var associatedProps = playsetObject.GetOrDefault("AssociatedPlaysetProps", Array.Empty<FSoftObjectPath>());
                foreach (var prop in associatedProps)
                {
                    var propName = prop.AssetPathName.Text.SubstringAfterLast(".");
                    propDictionary[propName] = playsetName.Text;
                }
            }
        }

        var items = AppVM.CUE4ParseVM.AssetDataBuffers.Where(x => ClassNames.Any(y => x.AssetClass.Text.Equals(y, StringComparison.OrdinalIgnoreCase))).ToList();

        // prioritize random first cuz of parallel list positions
        var random = items.FirstOrDefault(x => x.AssetName.Text.Contains("Random", StringComparison.OrdinalIgnoreCase));
        if (random is not null && AssetType != EAssetType.Prop)
        {
            items.Remove(random);
            await DoLoad(random, AssetType, true);
        }

        var addedAssets = new List<string>();
        await Parallel.ForEachAsync(items, async (data, token) =>
        {
            var displayName = string.Empty;
            if (data.TagsAndValues.TryGetValue("DisplayName", out var displayNameRaw))
            {
                displayName = displayNameRaw.SubstringBeforeLast('"').SubstringAfterLast('"').Trim();
            }

            // Weapon Filtering
            if (AssetType is EAssetType.Weapon)
            {
                var objectData = await AppVM.CUE4ParseVM.Provider.LoadObjectAsync(data.ObjectPath);
                var foundMappings = AppSettings.Current.WeaponMappings.TryGetValue(displayName, out var weaponMeshPaths);
                if (!foundMappings)
                {
                    var mainWeapon = ExportHelpers.GetWeaponMeshes(objectData).FirstOrDefault();
                    if (mainWeapon is null) return;

                    if (!AppSettings.Current.WeaponMappings.ContainsKey(displayName))
                    {
                        AppSettings.Current.WeaponMappings[displayName] = new List<string>();
                    }

                    AppSettings.Current.WeaponMappings[displayName].AddUnique(mainWeapon.GetPathName());
                    weaponMeshPaths = AppSettings.Current.WeaponMappings[displayName];
                }

                foreach (var weaponMesh in weaponMeshPaths.ToArray())
                {
                    if (addedAssets.ToArray().Contains(weaponMesh)) continue;
                    addedAssets.Add(weaponMesh);
                    await DoLoad(objectData, AssetType);
                }

                return;
            }

            // Prop Filtering
            if (AssetType is EAssetType.Prop)
            {
                if (addedAssets.ToArray().Contains(displayName))
                {
                    return;
                }

                addedAssets.Add(displayName);
                if (propDictionary.TryGetValue(data.AssetName.Text, out var gallery))
                {
                    await DoLoad(data, AssetType, descriptionOverride: gallery);
                }
                else
                {
                    await DoLoad(data, AssetType, descriptionOverride: "Unknown Gallery");
                }
                return;
            }
            
            if (AssetType is EAssetType.Vehicle)
            {
                await DoLoad(data, AssetType, descriptionOverride: data.AssetName.Text);
                return;
            }

            await DoLoad(data, AssetType);

        });
        sw.Stop();
        AppLog.Information($"Loaded {AssetType.GetDescription()} in {Math.Round(sw.Elapsed.TotalSeconds, 2)}s");
    }

    private async Task DoLoad(FAssetData data, EAssetType type, bool random = false, string? descriptionOverride = null)
    {
        var asset = await AppVM.CUE4ParseVM.Provider.LoadObjectAsync(data.ObjectPath);
        await DoLoad(asset, type, random, descriptionOverride);
    }

    private async Task DoLoad(UObject asset, EAssetType type, bool random = false, string? descriptionOverride = null)
    {
        try
        {
            await PauseState.WaitIfPaused();

            var previewImage = IconGetter(asset);
            previewImage ??= AppVM.CUE4ParseVM.PlaceholderTexture;
            if (previewImage is null) return;

            await Application.Current.Dispatcher.InvokeAsync(() => TargetCollection.Add(new AssetSelectorItem(asset, previewImage, type, random, DisplayNameGetter?.Invoke(asset), descriptionOverride, RemoveList.Any(y => asset.Name.Contains(y, StringComparison.OrdinalIgnoreCase)))), DispatcherPriority.Background);
        }
        catch (Exception e)
        {
            Log.Error("Failed to load {ObjectPath}", asset.GetPathName());
            Log.Debug(e.Message + e.StackTrace);
        }
    }
}