using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Services;
using SkiaSharp;

namespace FortnitePorting.Models.Assets.Loading;

public partial class AssetLoaderCollection : ObservableObject
{
    public static AssetLoaderCollection CategoryAccessor = new(false);
    
    public AssetLoader[] Loaders => Categories.SelectMany(category => category.Loaders).ToArray();
    
    public List<AssetLoaderCategory> Categories { get; set; } =
    [
        new AssetLoaderCategory(EAssetCategory.Cosmetics)
        {
            Loaders = 
            [
                new AssetLoader(EExportType.Outfit)
                {
                    ClassNames = ["AthenaCharacterItemDefinition"],
                    HideNames = ["Bean_", "BeanCharacter"],
                    PlaceholderIconPath = "FortniteGame/Content/Athena/Prototype/Textures/T_Placeholder_Item_Outfit",
                    IconHandler = asset =>
                    {
                        var previewImage = AssetLoader.GetIcon(asset);
                        if (previewImage is null && asset.TryGetValue(out UObject hero, "HeroDefinition"))
                            previewImage = AssetLoader.GetIcon(hero);

                        return previewImage;
                    }
                },
                new AssetLoader(EExportType.Backpack)
                {
                    ClassNames = ["AthenaBackpackItemDefinition"],
                    HideNames = ["_STWHeroNoDefaultBackpack", "_TEST", "Dev_", "_NPC", "_TBD"]
                },
                new AssetLoader(EExportType.Pickaxe)
                {
                    ClassNames = ["AthenaPickaxeItemDefinition"],
                    HideNames = ["Dev_", "TBD_"],
                    IconHandler = asset =>
                    {
                        var previewImage = AssetLoader.GetIcon(asset);
                        if (previewImage is null && asset.TryGetValue(out UObject hero, "WeaponDefinition"))
                            previewImage = AssetLoader.GetIcon(hero);

                        return previewImage;
                    }
                },
                new AssetLoader(EExportType.Glider)
                {
                    ClassNames = ["AthenaGliderItemDefinition"]
                },
                new AssetLoader(EExportType.Pet)
                {
                    ClassNames = ["AthenaPetCarrierItemDefinition"]
                },
                new AssetLoader(EExportType.Toy)
                {
                    ClassNames = ["AthenaToyItemDefinition"]
                },
                new AssetLoader(EExportType.Emoticon)
                {
                    ClassNames = ["AthenaEmojiItemDefinition"],
                    HideNames = ["Emoji_100APlus"]
                },
                new AssetLoader(EExportType.Spray)
                {
                    ClassNames = ["AthenaSprayItemDefinition"],
                    HideNames = ["SPID_000", "SPID_001"]
                },
                new AssetLoader(EExportType.Banner)
                {
                    ClassNames = ["FortHomebaseBannerIconItemDefinition"],
                    HideRarity = true
                },
                new AssetLoader(EExportType.LoadingScreen)
                {
                    ClassNames = ["AthenaLoadingScreenItemDefinition"]
                },
                new AssetLoader(EExportType.Emote)
                {
                    ClassNames = ["AthenaDanceItemDefinition"],
                    HideNames = ["_CT", "_NPC"]
                }
            ]
        },
        new AssetLoaderCategory(EAssetCategory.Creative)
        {
            Loaders = 
            [
                new AssetLoader(EExportType.Prop)
                {
                    ClassNames = ["FortPlaysetPropItemDefinition"],
                    HideRarity = true,
                    HidePredicate = (loader, asset, name) =>
                    {
                        // if prop has already been filtered, dont load it
                        // then compare names w/ the already loaded assets
                        // if they have the same name, they're a duplicate and should be filtered
                        // slow on first load, otherwise a LOT faster
                        // filtered assets are put into styles of main asset on click
                        
                        if (loader.FilteredAssetBag.Contains(name))
                        {
                            return true;
                        }
                        
                        loader.FilteredAssetBag.Add(name);
                        return false;
                    },
                    AddStyleHandler = (loader, asset, name) =>
                    {
                        var path = asset.GetPathName();
                        loader.StyleDictionary.TryAdd(name, []);
                        loader.StyleDictionary[name].Add(path);
                    }
                },
                new AssetLoader(EExportType.Prefab)
                {
                    ClassNames = ["FortPlaysetItemDefinition"],
                    HideNames = ["Device", "PID_Playset", "PID_MapIndicator", "SpikyStadium", "PID_StageLight", "PID_Temp_Island",
                                "PID_LimeEmptyPlot", "PID_Townscaper", "JunoPlotPlaysetItemDefintion", "LME",
                                "PID_ObstacleCourse", "MW_"],
                    HideRarity = true,
                    GameplayTagHandler = asset =>
                    {
                        var tagsHelper = asset.GetOrDefault<FStructFallback?>("CreativeTagsHelper");
                        var tags = tagsHelper?.GetOrDefault<FName[]>("CreativeTags") ?? [];
                        var gameplayTags = tags.Select(tag => new FGameplayTag(tag)).ToArray();
                        return new FGameplayTagContainer(gameplayTags);
                    }
                }
            ]
        },
        new AssetLoaderCategory(EAssetCategory.Gameplay)
        {
            Loaders = 
            [
                new AssetLoader(EExportType.Item)
                {
                    ClassNames = ["AthenaGadgetItemDefinition", "FortWeaponRangedItemDefinition", 
                        "FortWeaponMeleeItemDefinition", "FortCreativeWeaponMeleeItemDefinition", 
                        "FortCreativeWeaponRangedItemDefinition", "FortWeaponMeleeDualWieldItemDefinition"],
                    HideNames = ["_Harvest", "Weapon_Pickaxe_", "Weapons_Pickaxe_", "Dev_WID"],
                    HidePredicate = (loader, asset, name) =>  false // TODO item filtering
                },
                new AssetLoader(EExportType.Resource)
                {
                    ClassNames = ["FortIngredientItemDefinition", "FortResourceItemDefinition"],
                    HideNames = ["SurvivorItemData", "OutpostUpgrade_StormShieldAmplifier"]
                },
                new AssetLoader(EExportType.Trap)
                {
                    ClassNames = ["FortTrapItemDefinition"],
                    HideNames = ["TID_Creative", "TID_Floor_Minigame_Trigger_Plate"],
                    HidePredicate = (loader, asset, name) =>  false // TODO trap filtering
                    
                },
                new AssetLoader(EExportType.Vehicle)
                {
                    ClassNames = ["FortVehicleItemDefinition"],
                    IconHandler = asset => asset.GetVehicleMetadata<UTexture2D>("Icon", "SmallPreviewImage", "LargePreviewImage"),
                    DisplayNameHandler = asset => asset.GetVehicleMetadata<FText>("DisplayName", "ItemName")?.Text,
                    HideRarity = true
                },
                new AssetLoader(EExportType.Wildlife)
                {
                    ManuallyDefinedAssets = 
                    [
                        new ManuallyDefinedAsset
                        {
                            Name = "Llama",
                            AssetPath = "/Labrador/Meshes/Labrador_Mammal",
                            IconPath = "FortniteGame/Content/UI/Foundation/Textures/Icons/Athena/T-T-Icon-BR-SM-Athena-SupplyLlama-01"
                        },
                        new ManuallyDefinedAsset
                        {
                            Name = "Boar",
                            AssetPath = "/Irwin/AI/Prey/Burt/Meshes/Burt_Mammal",
                            IconPath = "/Irwin/Icons/T-Icon-Fauna-Boar"
                        },
                        new ManuallyDefinedAsset
                        {
                            Name = "Chicken",
                            AssetPath = "/Irwin/AI/Prey/Nug/Meshes/Nug_Bird",
                            IconPath = "/Irwin/Icons/T-Icon-Fauna-Chicken"
                        },
                        new ManuallyDefinedAsset
                        {
                            Name = "Frog",
                            AssetPath = "/Irwin/AI/Simple/Smackie/Meshes/Smackie_Amphibian",
                            IconPath = "/Irwin/Icons/T-Icon-Fauna-Frog"
                        },
                        new ManuallyDefinedAsset
                        {
                            Name = "Crow",
                            AssetPath = "/Irwin/AI/Prey/Crow/Meshes/Crow_Bird",
                            IconPath = "/Irwin/Icons/T-Icon-Fauna-Crow"
                        },
                        new ManuallyDefinedAsset
                        {
                            Name = "Raptor",
                            AssetPath = "/Irwin/AI/Predators/Robert/Meshes/Jungle_Raptor_Fauna",
                            IconPath = "/Irwin/Icons/T-Icon-Fauna-JungleRaptor"
                        },
                        new ManuallyDefinedAsset
                        {
                            Name = "Wolf",
                            AssetPath = "/Irwin/AI/Predators/Grandma/Meshes/Grandma_Mammal",
                            IconPath = "/Irwin/Icons/T-Icon-Fauna-Wolf"
                        }
                    ],
                    CustomAssets = 
                    [
                        new CustomAsset
                        {
                            Name = "Oshawott",
                            Description = "No Description.",
                            IconBitmap = SKBitmap.Decode(Avalonia.Platform.AssetLoader.Open(new Uri("avares://FortnitePorting/Assets/Custom/T_Oshawott-L.png")))
                        }
                    ],
                    HideRarity = true
                }
            ]
        },
        new AssetLoaderCategory(EAssetCategory.Festival)
        {
            Loaders = 
            [
                new AssetLoader(EExportType.FestivalGuitar)
                {
                    ClassNames = ["SparksGuitarItemDefinition"]
                },
                new AssetLoader(EExportType.FestivalBass)
                {
                    ClassNames = ["SparksBassItemDefinition"]
                },
                new AssetLoader(EExportType.FestivalKeytar)
                {
                    ClassNames = ["SparksKeyboardItemDefinition"]
                },
                new AssetLoader(EExportType.FestivalDrum)
                {
                    ClassNames = ["SparksDrumItemDefinition"]
                },
                new AssetLoader(EExportType.FestivalMic)
                {
                    ClassNames = ["SparksMicItemDefinition"]
                },
            ]
        },
        /*new AssetLoaderCategory(EAssetCategory.Lego)
        {
            Loaders = 
            [
                new AssetLoader(EExportType.LegoOutfit)
                {
                    ClassNames = ["JunoAthenaCharacterItemOverrideDefinition"],
                    IconHandler = asset =>
                    {
                        var meshSchema = asset.GetAnyOrDefault<UObject?>("AssembledMeshSchema", "LowDetailsAssembledMeshSchema");
                        if (meshSchema is null) return null;

                        var additionalDatas = meshSchema.GetOrDefault("AdditionalData", Array.Empty<FInstancedStruct>());
                        foreach (var additionalData in additionalDatas)
                        {
                            var previewImage = additionalData.NonConstStruct?.GetAnyOrDefault<UTexture2D?>("SmallPreviewImage", "LargePreviewImage");
                            if (previewImage is not null) return previewImage;
                        }

                        return null;
                    },
                    DisplayNameHandler = asset =>
                    {
                        var baseItemDefinition = asset.GetOrDefault<UObject?>("BaseAthenaCharacterItemDefinition");
                        return baseItemDefinition?.GetAnyOrDefault<FText?>("DisplayName", "ItemName")?.Text ?? asset.Name;
                    },
                    DescriptionHandler = asset =>
                    {
                        var baseItemDefinition = asset.GetOrDefault<UObject?>("BaseAthenaCharacterItemDefinition");
                        return baseItemDefinition?.GetAnyOrDefault<FText?>("Description", "ItemDescription")?.Text ?? "No description.";
                    }
                },
                new AssetLoader(EExportType.LegoEmote)
                {
                    ClassNames = ["JunoAthenaCharacterItemOverrideDefinition"],
                    IconHandler = asset =>
                    {
                        var baseItemDefinition = asset.GetOrDefault<UObject?>("BaseAthenaDanceItemDefinition");
                        return baseItemDefinition?.GetAnyOrDefault<UTexture2D?>("SmallPreviewImage", "LargePreviewImage");
                    },
                    DisplayNameHandler = asset =>
                    {
                        var baseItemDefinition = asset.GetOrDefault<UObject?>("BaseAthenaDanceItemDefinition");
                        return baseItemDefinition?.GetAnyOrDefault<FText?>("DisplayName", "ItemName")?.Text ?? asset.Name;
                    },
                    DescriptionHandler = asset =>
                    {
                        var baseItemDefinition = asset.GetOrDefault<UObject?>("BaseAthenaDanceItemDefinition");
                        return baseItemDefinition?.GetAnyOrDefault<FText?>("Description", "ItemDescription")?.Text ?? "No description.";
                    }
                }
            ]
        }*/
        new AssetLoaderCategory(EAssetCategory.FallGuys)
        {
            Loaders = 
            [
                new AssetLoader(EExportType.FallGuysOutfit)
                {
                    ClassNames = ["AthenaCharacterItemDefinition"],
                    AllowNames = ["Bean_"],
                    PlaceholderIconPath = "FortniteGame/Content/Athena/Prototype/Textures/T_Placeholder_Item_Outfit",
                    HideRarity = true
                }
            ]
        }
    ];
    
    [ObservableProperty] private ObservableCollection<NavigationViewItem> _navItems = [];
    [ObservableProperty] private NavigationViewItem _selectedNavItem;
    
    [ObservableProperty] private AssetLoader _activeLoader;
    [ObservableProperty] private ReadOnlyObservableCollection<AssetItem> _activeCollection;

    public AssetLoaderCollection(bool isForUi = true)
    {
        if (!isForUi) return;
        
        TaskService.RunDispatcher(() =>
        {
            foreach (var category in Categories)
            {
                NavItems.Add(new NavigationViewItem
                {
                    Tag = category.Category,
                    Content = category.Category.GetDescription(),
                    SelectsOnInvoked = false,
                    IconSource = new ImageIconSource
                    {
                        Source = ImageExtensions.AvaresBitmap($"avares://FortnitePorting/Assets/FN/{category.Category.ToString()}.png")
                    },
                    MenuItemsSource = category.Loaders.Select(loader => new NavigationViewItem
                    {
                        Tag = loader.Type, 
                        Content = loader.Type.GetDescription(), 
                        IconSource = new ImageIconSource
                        {
                            Source = ImageExtensions.AvaresBitmap($"avares://FortnitePorting/Assets/FN/{loader.Type.ToString()}.png")
                        },
                    })
                });
            }
        });
    }
    
    public async Task Load(EExportType type)
    {
        Set(type);
        await ActiveLoader.Load();
    }
    
    public void Set(EExportType type)
    {
        ActiveLoader = Get(type);
        ActiveCollection = ActiveLoader.Filtered;
        ActiveLoader.UpdateFilterVisibility();
    }

    public AssetLoader Get(EExportType type)
    {
        foreach (var category in Categories)
        {
            if (category.Loaders.FirstOrDefault(loader => loader.Type == type) is { } assetLoader)
            {
                return assetLoader;
            }
        }

        return null!; // if this happens it's bc im stupid
    }
}