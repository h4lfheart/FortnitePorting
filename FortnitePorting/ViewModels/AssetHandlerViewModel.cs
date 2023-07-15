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
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.AppUtils;
using FortnitePorting.Exports;
using FortnitePorting.Models;
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
            { EAssetType.Item, ItemHandler },
            { EAssetType.Dance, DanceHandler },
            { EAssetType.Vehicle, VehicleHandler },
            { EAssetType.Gallery, GalleryHandler },
            { EAssetType.Prop, PropHandler },
            { EAssetType.Pet, PetHandler },
            { EAssetType.Music, MusicPackHandler },
            { EAssetType.Toy, ToyHandler },
            { EAssetType.Wildlife, WildlifeHandler },
            { EAssetType.Trap, TrapHandler }
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

    private readonly AssetHandlerData ItemHandler = new()
    {
        AssetType = EAssetType.Item,
        TargetCollection = AppVM.MainVM.Items,
        ClassNames = new List<string> { "AthenaGadgetItemDefinition", "FortWeaponRangedItemDefinition", "FortWeaponMeleeItemDefinition", "FortCreativeWeaponMeleeItemDefinition", "FortCreativeWeaponRangedItemDefinition" },
        RemoveList = { "_Harvest", "Weapon_Pickaxe_", "Weapons_Pickaxe_", "Dev_WID", "Random_Cosmetic_Pickaxe" },
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

    private readonly AssetHandlerData GalleryHandler = new()
    {
        AssetType = EAssetType.Gallery,
        IconGetter = asset => asset.GetOrDefault<UTexture2D?>("SmallPreviewImage", "LargePreviewImage")
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

    private readonly AssetHandlerData ToyHandler = new()
    {
        AssetType = EAssetType.Toy,
        TargetCollection = AppVM.MainVM.Toys,
        ClassNames = new List<string> { "AthenaToyItemDefinition" },
        RemoveList = { },
        IconGetter = asset => asset.GetOrDefault<UTexture2D?>("SmallPreviewImage", "LargePreviewImage")
    };

    private readonly AssetHandlerData WildlifeHandler = new()
    {
        AssetType = EAssetType.Wildlife,
        TargetCollection = AppVM.MainVM.Wildlife
    };
    
    private readonly AssetHandlerData TrapHandler = new()
    {
        AssetType = EAssetType.Trap,
        TargetCollection = AppVM.MainVM.Traps,
        ClassNames = new List<string> { "FortTrapItemDefinition" },
        RemoveList = { },
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

        var galleryMappings = new List<GalleryData>();
        if (AssetType is EAssetType.Gallery)
        {
            var addedProps = new List<string>();
            var playsets = AppVM.CUE4ParseVM.AssetDataBuffers.Where(x => x.AssetClass.Text.Equals("FortPlaysetItemDefinition", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var playset in playsets)
            {
                await PauseState.WaitIfPaused();
                var playsetObject = await AppVM.CUE4ParseVM.Provider.LoadObjectAsync(playset.ObjectPath);

                var creativeTagsHelper = playsetObject.GetOrDefault("CreativeTagsHelper", new FStructFallback());
                var creativeTags = creativeTagsHelper.GetOrDefault("CreativeTags", Array.Empty<FName>());
                if (!creativeTags.Any(x => x.Text.Contains("Gallery", StringComparison.OrdinalIgnoreCase) || x.Text.Contains("Prefab", StringComparison.OrdinalIgnoreCase))) continue;

                var playsetName = playsetObject.GetOrDefault("DisplayName", new FText("Unknown Gallery")).Text;
                var associatedProps = playsetObject.GetOrDefault("AssociatedPlaysetProps", Array.Empty<FSoftObjectPath>());
                if (associatedProps.Length == 0) continue;

                var galleryData = new GalleryData(playsetName, playsetObject.Name, playsetObject.GetPathName());
                galleryData.Props.AddRange(associatedProps.Select(x => x.AssetPathName.Text));
                galleryMappings.Add(galleryData);

                playsetObject.TryGetValue(out UTexture2D? playsetImage, "SmallPreviewImage", "LargePreviewImage");
                playsetImage ??= AppVM.CUE4ParseVM.PlaceholderTexture;
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var propExpander = new PropExpander(playsetName, playsetImage);
                    galleryData.Expander = propExpander;
                    AppVM.MainVM.Galleries.Add(propExpander);
                }, DispatcherPriority.Background);

                await Parallel.ForEachAsync(associatedProps, async (data, token) =>
                {
                    var associatedProp = await data.LoadAsync();
                    var displayName = associatedProp.GetOrDefault("DisplayName", new FText(associatedProp.Name)).Text;
                    if (addedProps.ToArray().Contains(displayName))
                    {
                        return;
                    }

                    addedProps.Add(displayName);
                    await DoLoadProps(galleryData.Expander.Props, associatedProp, AssetType, descriptionOverride: galleryData.Name);
                });

                if (galleryData.Expander.Props.Count == 0) AppVM.MainVM.Galleries.Remove(galleryData.Expander);
            }

            sw.Stop();
            Log.Information($"Loaded {AssetType.GetDescription()} in {Math.Round(sw.Elapsed.TotalSeconds, 2)}s");
            return;
        }

        if (AssetType is EAssetType.Wildlife)
        {
            await DoLoadWildlife("Boar",
                "FortniteGame/Plugins/GameFeatures/Irwin/Content/AI/Prey/Burt/Meshes/Burt_Mammal",
                "FortniteGame/Plugins/GameFeatures/Irwin/Content/Icons/T-Icon-Fauna-Boar");

            await DoLoadWildlife("Chicken",
                "FortniteGame/Plugins/GameFeatures/Irwin/Content/AI/Prey/Nug/Meshes/Nug_Bird",
                "FortniteGame/Plugins/GameFeatures/Irwin/Content/Icons/T-Icon-Fauna-Chicken");

            await DoLoadWildlife("Crow",
                "FortniteGame/Plugins/GameFeatures/Irwin/Content/AI/Prey/Crow/Meshes/Crow_Bird",
                "FortniteGame/Plugins/GameFeatures/Irwin/Content/Icons/T-Icon-Fauna-Crow");

            await DoLoadWildlife("Frog",
                "FortniteGame/Plugins/GameFeatures/Irwin/Content/AI/Simple/Smackie/Meshes/Smackie_Amphibian",
                "FortniteGame/Plugins/GameFeatures/Irwin/Content/Icons/T-Icon-Fauna-Frog");

            await DoLoadWildlife("Raptor",
                "FortniteGame/Plugins/GameFeatures/Irwin/Content/AI/Predators/Robert/Meshes/Jungle_Raptor_Fauna",
                "FortniteGame/Plugins/GameFeatures/Irwin/Content/Icons/T-Icon-Fauna-JungleRaptor");

            await DoLoadWildlife("Wolf",
                "FortniteGame/Plugins/GameFeatures/Irwin/Content/AI/Predators/Grandma/Meshes/Grandma_Mammal",
                "FortniteGame/Plugins/GameFeatures/Irwin/Content/Icons/T-Icon-Fauna-Wolf");

            return;
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
            try
            {
                var displayName = data.AssetName.Text;
                if (data.TagsAndValues.TryGetValue("DisplayName", out var displayNameRaw))
                {
                    displayName = displayNameRaw.SubstringBeforeLast('"').SubstringAfterLast('"').Trim();
                }

                // Item Filtering
                if (AssetType is EAssetType.Item)
                {
                    var objectData = await AppVM.CUE4ParseVM.Provider.LoadObjectAsync(data.ObjectPath);
                    var foundMappings = AppSettings.Current.ItemMapppings.TryGetValue(displayName, out var weaponMeshPaths);
                    if (!foundMappings)
                    {
                        var mainWeapon = ExportHelpers.GetWeaponMeshes(objectData).FirstOrDefault();
                        if (mainWeapon is null) return;

                        if (!AppSettings.Current.ItemMapppings.ContainsKey(displayName))
                        {
                            AppSettings.Current.ItemMapppings.Add(displayName, new List<string>());
                        }

                        AppSettings.Current.ItemMapppings[displayName].AddUnique(mainWeapon.GetPathName());
                        weaponMeshPaths = AppSettings.Current.ItemMapppings[displayName];
                    }

                    foreach (var weaponMesh in weaponMeshPaths.ToArray())
                    {
                        if (addedAssets.ToArray().Contains(weaponMesh) && AppSettings.Current.FilterItems) continue;
                        addedAssets.Add(weaponMesh);
                        await DoLoad(objectData, AssetType);
                    }

                    return;
                }

                // Prop Filtering
                if (AssetType is EAssetType.Prop)
                {
                    if (addedAssets.ToArray().Contains(displayName) && AppSettings.Current.FilterProps)
                    {
                        return;
                    }

                    addedAssets.Add(displayName);
                    var foundGallery = galleryMappings.FirstOrDefault(gallery => gallery.Props.Any(prop => prop.Contains(data.AssetName.Text)));
                    if (foundGallery is not null)
                    {
                        await DoLoad(data, AssetType, descriptionOverride: foundGallery.Name);
                    }
                    else
                    {
                        await DoLoad(data, AssetType, descriptionOverride: "Unknown Gallery");
                    }

                    return;
                }
                
                // Trap Filtering
                if (AssetType is EAssetType.Trap)
                {
                    if (addedAssets.ToArray().Contains(displayName))
                    {
                        return;
                    }

                    addedAssets.Add(displayName);
                    await DoLoad(data, AssetType);
                    return;
                }

                if (AssetType is EAssetType.Vehicle)
                {
                    await DoLoad(data, AssetType, descriptionOverride: data.AssetName.Text);
                    return;
                }

                await DoLoad(data, AssetType);
            }
            catch (Exception e)
            {
                Log.Error("Failed to load {ObjectPath}", data.ObjectPath);
                Log.Error(e.Message + e.StackTrace);
            }
        });
        sw.Stop();
        Log.Information($"Loaded {AssetType.GetDescription()} in {Math.Round(sw.Elapsed.TotalSeconds, 2)}s");
    }

    private async Task DoLoad(FAssetData data, EAssetType type, bool random = false, string? descriptionOverride = null)
    {
        var asset = await AppVM.CUE4ParseVM.Provider.LoadObjectAsync(data.ObjectPath);
        await DoLoad(asset, type, random, descriptionOverride);
    }

    private async Task DoLoad(UObject asset, EAssetType type, bool random = false, string? descriptionOverride = null)
    {
        await PauseState.WaitIfPaused();

        var previewImage = IconGetter(asset);
        previewImage ??= AppVM.CUE4ParseVM.PlaceholderTexture;
        if (previewImage is null) return;

        await Application.Current.Dispatcher.InvokeAsync(() => TargetCollection.Add(new AssetSelectorItem(asset, previewImage, type, random, DisplayNameGetter?.Invoke(asset), descriptionOverride, RemoveList.Any(y => asset.Name.Contains(y, StringComparison.OrdinalIgnoreCase)))), DispatcherPriority.Background);
    }

    private async Task DoLoadProps(ObservableCollection<AssetSelectorItem> target, UObject asset, EAssetType type, bool random = false, string? descriptionOverride = null)
    {
        await PauseState.WaitIfPaused();

        var previewImage = IconGetter(asset);
        previewImage ??= AppVM.CUE4ParseVM.PlaceholderTexture;
        if (previewImage is null) return;

        await Application.Current.Dispatcher.InvokeAsync(() => target.Add(new AssetSelectorItem(asset, previewImage, type, random, DisplayNameGetter?.Invoke(asset), descriptionOverride, RemoveList.Any(y => asset.Name.Contains(y, StringComparison.OrdinalIgnoreCase)))), DispatcherPriority.Background);
    }

    private async Task DoLoadWildlife(string name, string skeletalMeshPath, string previewImagePath)
    {
        await PauseState.WaitIfPaused();

        var skeletalMesh = await AppVM.CUE4ParseVM.Provider.LoadObjectAsync<USkeletalMesh>(skeletalMeshPath);
        var previewImage = await AppVM.CUE4ParseVM.Provider.LoadObjectAsync<UTexture2D>(previewImagePath);

        await Application.Current.Dispatcher.InvokeAsync(() => AppVM.MainVM.Wildlife.Add(new AssetSelectorItem(skeletalMesh, previewImage, EAssetType.Wildlife, false, new FText(name))), DispatcherPriority.Background);
    }
}