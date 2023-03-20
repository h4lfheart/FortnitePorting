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
            { EAssetType.Weapon, WeaponHandler },
            { EAssetType.Dance, DanceHandler },
            { EAssetType.Vehicle, VehicleHandler },
            { EAssetType.Gallery, GalleryHandler },
            { EAssetType.Prop, PropHandler },
            { EAssetType.Pet, PetHandler },
            { EAssetType.Music, MusicPackHandler },
            { EAssetType.Toy, ToyHandler }
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
        
        if (AssetType is EAssetType.Gallery && AppSettings.Current.GalleryMappings.Count == 0)
        {
            AppLog.Warning("Generating first-time prop gallery mappings, this may take longer than usual");
        }

        // prop asset name : gallery
        if (AssetType is EAssetType.Gallery)
        {
            var addedProps = new List<string>();
            var playsets = AppVM.CUE4ParseVM.AssetDataBuffers.Where(x => x.AssetClass.Text.Equals("FortPlaysetItemDefinition", StringComparison.OrdinalIgnoreCase)).ToList();
            //playsets.RemoveAll(playset => AppSettings.Current.GalleryMappings.Any(gallery => gallery.ID.Equals(playset.AssetName.Text)));
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
                AppSettings.Current.GalleryMappings.Add(galleryData);
                
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
            AppLog.Information($"Loaded {AssetType.GetDescription()} in {Math.Round(sw.Elapsed.TotalSeconds, 2)}s");
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
                var foundGallery = AppSettings.Current.GalleryMappings.FirstOrDefault(gallery => gallery.Props.Any(prop => prop.Contains(data.AssetName.Text)));
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

    private async Task DoLoadProps(ObservableCollection<AssetSelectorItem> target, UObject asset, EAssetType type, bool random = false, string? descriptionOverride = null)
    {
        try
        {
            await PauseState.WaitIfPaused();

            var previewImage = IconGetter(asset);
            previewImage ??= AppVM.CUE4ParseVM.PlaceholderTexture;
            if (previewImage is null) return;

            await Application.Current.Dispatcher.InvokeAsync(() => target.Add(new AssetSelectorItem(asset, previewImage, type, random, DisplayNameGetter?.Invoke(asset), descriptionOverride, RemoveList.Any(y => asset.Name.Contains(y, StringComparison.OrdinalIgnoreCase)))), DispatcherPriority.Background);
        }
        catch (Exception e)
        {
            Log.Error("Failed to load {ObjectPath}", asset.GetPathName());
            Log.Debug(e.Message + e.StackTrace);
        }
    }
}
