using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.GameTypes.FN.Enums;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using DynamicData;
using DynamicData.Binding;
using FortnitePorting.Application;
using FortnitePorting.Controls.Assets;
using FortnitePorting.Export;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.Framework.Services;
using Material.Icons;
using ReactiveUI;
using Serilog;

namespace FortnitePorting.ViewModels;

public partial class AssetsViewModel : ViewModelBase
{
    public List<AssetLoader> Loaders;
    [ObservableProperty] private AssetLoader? currentLoader;
    [ObservableProperty] private Control expanderContainer;

    [ObservableProperty] private ObservableCollection<AssetOptions> currentAssets = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCosmeticFilters))]
    [NotifyPropertyChangedFor(nameof(HasGameFilters))] 
    [NotifyPropertyChangedFor(nameof(HasPrefabFilters))] 
    [NotifyPropertyChangedFor(nameof(HasItemFilters))] 
    private EAssetType currentAssetType;
    public bool HasCosmeticFilters => CosmeticFilterTypes.Contains(CurrentAssetType);
    private readonly EAssetType[] CosmeticFilterTypes =
    [
        EAssetType.Outfit,
        EAssetType.Backpack,
        EAssetType.Pickaxe,
        EAssetType.Glider,
        EAssetType.Pet,
        EAssetType.Toy,
        EAssetType.Emote,
        EAssetType.Emoticon,
        EAssetType.Spray,
        EAssetType.LoadingScreen
    ];
    
    public bool HasGameFilters => GameFilterTypes.Contains(CurrentAssetType);
    private readonly EAssetType[] GameFilterTypes =
    [
        EAssetType.Outfit,
        EAssetType.Backpack,
        EAssetType.Pickaxe,
        EAssetType.Glider,
        EAssetType.Banner,
        EAssetType.LoadingScreen,
        EAssetType.Item,
        EAssetType.Resource,
        EAssetType.Trap
    ];
    
    public bool HasPrefabFilters => CurrentAssetType is EAssetType.Prefab;
    public bool HasItemFilters => CurrentAssetType is EAssetType.Item;

    [ObservableProperty] private int exportChunks;
    [ObservableProperty] private int exportProgress;
    [ObservableProperty] private bool isExporting;
    [ObservableProperty] private EExportTargetType exportType = EExportTargetType.Blender;
    [ObservableProperty] private ReadOnlyObservableCollection<AssetItem> activeCollection;

    [ObservableProperty] private ESortType sortType = ESortType.Default;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(SortIcon))]
    private bool isDescending;

    [ObservableProperty] private string searchFilter = string.Empty;
    [ObservableProperty] private string filterPreviewText = "None";
    [ObservableProperty] private AvaloniaDictionary<string, Predicate<AssetItem>> filters = new();
    public MaterialIconKind SortIcon => IsDescending ? MaterialIconKind.SortDescending : MaterialIconKind.SortAscending;
    public readonly IObservable<SortExpressionComparer<AssetItem>> AssetSort;
    public readonly IObservable<Func<AssetItem, bool>> AssetFilter;

    public static readonly Dictionary<string, Predicate<AssetItem>> FilterPredicates = new()
    {
        { "Favorite", x => x.IsFavorite },
        { "Hidden Assets", x => x.Hidden },
        { "Battle Pass", x => x.GameplayTags.ContainsAny("BattlePass") },
        { "Item Shop", x => x.GameplayTags.ContainsAny("ItemShop") },
        { "Save The World", x => x.GameplayTags.ContainsAny("CampaignHero", "SaveTheWorld") || x.Asset.GetPathName().Contains("SaveTheWorld", StringComparison.OrdinalIgnoreCase) },
        { "Battle Royale", x => !x.GameplayTags.ContainsAny("CampaignHero", "SaveTheWorld") && !x.Asset.GetPathName().Contains("SaveTheWorld", StringComparison.OrdinalIgnoreCase) },
        { "Galleries", x => x.GameplayTags.ContainsAny("Gallery") },
        { "Prefabs", x => x.GameplayTags.ContainsAny("Prefab") },
        { "Weapons", x => x.GameplayTags.ContainsAny("Weapon") },
        { "Gadgets", x => x.Asset.ExportType.Equals("AthenaGadgetItemDefinition", StringComparison.OrdinalIgnoreCase) },
        { "Melee", x => x.GameplayTags.ContainsAny("Melee") },
        { "Consumables", x => x.GameplayTags.ContainsAny("Consume") },
        { "Lego", x => x.GameplayTags.ContainsAny("Juno") },
        
    };

    public AssetsViewModel()
    {
        AssetFilter = this.WhenAnyValue(x => x.SearchFilter, x => x.Filters).Select(CreateAssetFilter);
        AssetSort = this.WhenAnyValue(x => x.SortType, x => x.IsDescending).Select(CreateAssetSort);
    }

    public override async Task Initialize()
    {
        Loaders = new List<AssetLoader>
        {
            new(EAssetType.Outfit)
            {
                Classes = new[] { "AthenaCharacterItemDefinition" },
                Filters = new[] { "_NPC", "_TBD", "CID_VIP", "_Creative", "_SG" },
                IconHandler = asset =>
                {
                    asset.TryGetValue(out UTexture2D? previewImage, "SmallPreviewImage", "LargePreviewImage");
                    if (previewImage is null && asset.TryGetValue(out UObject heroDef, "HeroDefinition"))
                    {
                        previewImage = AssetLoader.GetAssetIcon(heroDef);
                        previewImage ??= heroDef.GetAnyOrDefault<UTexture2D>("SmallPreviewImage", "LargePreviewImage");

                    }
                    previewImage ??= AssetLoader.GetAssetIcon(asset);
                    return previewImage;
                }
            },
            new(EAssetType.LegoOutfit)
            {
                Classes = ["JunoAthenaCharacterItemOverrideDefinition"],
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
                    if (baseItemDefinition is null) return new FText(asset.Name);

                    return baseItemDefinition.GetAnyOrDefault<FText?>("DisplayName", "ItemName") ?? new FText(asset.Name);
                },
                DescriptionHandler = asset =>
                {
                    var baseItemDefinition = asset.GetOrDefault<UObject?>("BaseAthenaCharacterItemDefinition");
                    if (baseItemDefinition is null) return new FText("No description.");

                    return baseItemDefinition.GetAnyOrDefault<FText?>("Description", "ItemDescription") ?? new FText("No description.");
                }
            },
            new(EAssetType.Backpack)
            {
                Classes = new[] { "AthenaBackpackItemDefinition" },
                Filters = new[] { "_STWHeroNoDefaultBackpack", "_TEST", "Dev_", "_NPC", "_TBD" }
            },
            new(EAssetType.Pickaxe)
            {
                Classes = new[] { "AthenaPickaxeItemDefinition" },
                Filters = new[] { "Dev_", "TBD_" },
                IconHandler = asset =>
                {
                    asset.TryGetValue(out UTexture2D? previewImage, "SmallPreviewImage", "LargePreviewImage");
                    if (asset.TryGetValue(out UObject heroDef, "WeaponDefinition"))
                    {
                        previewImage = AssetLoader.GetAssetIcon(heroDef);
                        previewImage ??= heroDef.GetAnyOrDefault<UTexture2D>("SmallPreviewImage", "LargePreviewImage");
                    }
                    previewImage ??= AssetLoader.GetAssetIcon(asset);
                    return previewImage;
                }
            },
            new(EAssetType.Glider)
            {
                Classes = new[] { "AthenaGliderItemDefinition" }
            },
            new(EAssetType.Pet)
            {
                Classes = new[] { "AthenaPetCarrierItemDefinition" }
            },
            new(EAssetType.Toy)
            {
                Classes = new[] { "AthenaToyItemDefinition" }
            },
            new(EAssetType.Emoticon)
            {
                Classes = new[] { "AthenaEmojiItemDefinition" },
                Filters = new[] { "Emoji_100APlus" }
            },
            new(EAssetType.Spray)
            {
                Classes = new[] { "AthenaSprayItemDefinition" },
                Filters = new[] { "SPID_000", "SPID_001" }
            },
            new(EAssetType.Banner)
            {
                Classes = new[] { "FortHomebaseBannerIconItemDefinition" },
                HideRarity = true
            },
            new(EAssetType.LoadingScreen)
            {
                Classes = new[] { "AthenaLoadingScreenItemDefinition" }
            },
            new(EAssetType.Emote)
            {
                Classes = new[] { "AthenaDanceItemDefinition" },
                Filters = new[] { "_CT", "_NPC" }
            },
            new(EAssetType.Prop)
            {
                Classes = new[] { "FortPlaysetPropItemDefinition" },
                HidePredicate = (loader, asset, name) =>
                {
                    if (!AppSettings.Current.FilterProps) return false;

                    var path = asset.GetPathName();
                    if (AppSettings.Current.HiddenPropPaths.Contains(path)) return true;
                    if (loader.LoadedAssetsForFiltering.Contains(name))
                    {
                        AppSettings.Current.HiddenPropPaths.Add(path);
                        return true;
                    }

                    loader.LoadedAssetsForFiltering.Add(name);
                    return false;
                },
                DontLoadHiddenAssets = true,
                HideRarity = true
            },
            new(EAssetType.Prefab)
            {
                Classes = new[] { "FortPlaysetItemDefinition" },
                Filters = new[]
                {
                    "Device", "PID_Playset", "PID_MapIndicator", "SpikyStadium", "PID_StageLight",
                    "PID_Temp_Island"
                },
                HidePredicate = (loader, asset, name) =>
                {
                    var tagsHelper = asset.GetOrDefault<FStructFallback?>("CreativeTagsHelper");
                    if (tagsHelper is null) return false;

                    var tags = tagsHelper.GetOrDefault("CreativeTags", Array.Empty<FName>());
                    return tags.Any(tag => tag.Text.Contains("Device", StringComparison.OrdinalIgnoreCase));
                },
                DontLoadHiddenAssets = true,
                HideRarity = true
            },
            new(EAssetType.Item)
            {
                Classes = new[] { "AthenaGadgetItemDefinition", "FortWeaponRangedItemDefinition", "FortWeaponMeleeItemDefinition", "FortCreativeWeaponMeleeItemDefinition", "FortCreativeWeaponRangedItemDefinition", "FortWeaponMeleeDualWieldItemDefinition" },
                Filters = new[] { "_Harvest", "Weapon_Pickaxe_", "Weapons_Pickaxe_", "Dev_WID" },
                HidePredicate = (loader, asset, name) =>
                {
                    if (!AppSettings.Current.FilterItems) return false;
                    var path = asset.GetPathName();
                    var mappings = AppSettings.Current.ItemMeshMappings.GetOrAdd(name, () => new Dictionary<string, string>());
                    if (mappings.TryGetValue(path, out var meshPath))
                    {
                        if (loader.LoadedAssetsForFiltering.Contains(meshPath)) return true;

                        loader.LoadedAssetsForFiltering.Add(meshPath);
                        return false;
                    }

                    var mesh = ExporterInstance.WeaponDefinitionMeshes(asset).FirstOrDefault();
                    if (mesh is null) return true;

                    meshPath = mesh.GetPathName();

                    var shouldSkip = mappings.Any(x => x.Value.Equals(meshPath, StringComparison.OrdinalIgnoreCase));
                    mappings[path] = meshPath;
                    loader.LoadedAssetsForFiltering.Add(meshPath);
                    return shouldSkip;
                },
                DontLoadHiddenAssets = true
            },
            new(EAssetType.Resource)
            {
                Classes = new[] { "FortIngredientItemDefinition", "FortResourceItemDefinition" },
                Filters = new[] { "SurvivorItemData", "OutpostUpgrade_StormShieldAmplifier" },
            },
            new(EAssetType.Trap)
            {
                Classes = new[] { "FortTrapItemDefinition" },
                Filters = new[] { "TID_Creative", "TID_Floor_Minigame_Trigger_Plate" },
                HidePredicate = (loader, asset, name) =>
                {
                    if (!AppSettings.Current.FilterTraps) return false;

                    var path = asset.GetPathName();
                    if (AppSettings.Current.HiddenTrapPaths.Contains(path)) return true;
                    if (loader.LoadedAssetsForFiltering.Contains(name))
                    {
                        AppSettings.Current.HiddenTrapPaths.Add(path);
                        return true;
                    }

                    loader.LoadedAssetsForFiltering.Add(name);
                    return false;
                },
                DontLoadHiddenAssets = true,
                HideRarity = true
            },
            new(EAssetType.Vehicle)
            {
                Classes = new[] { "FortVehicleItemDefinition" },
                IconHandler = asset => GetVehicleInfo<UTexture2D>(asset, "SmallPreviewImage", "LargePreviewImage", "Icon"),
                DisplayNameHandler = asset => GetVehicleInfo<FText>(asset, "DisplayName", "ItemName"),
                HideRarity = true
            },
            new(EAssetType.Wildlife)
            {
                CustomLoadingHandler = async loader =>
                {
                    var entries = new[]
                    {
                        await ManualAssetItemEntry.Create("Boar",
                            "FortniteGame/Plugins/GameFeatures/Irwin/Content/AI/Prey/Burt/Meshes/Burt_Mammal",
                            "FortniteGame/Plugins/GameFeatures/Irwin/Content/Icons/T-Icon-Fauna-Boar"),

                        await ManualAssetItemEntry.Create("Chicken",
                            "FortniteGame/Plugins/GameFeatures/Irwin/Content/AI/Prey/Nug/Meshes/Nug_Bird",
                            "FortniteGame/Plugins/GameFeatures/Irwin/Content/Icons/T-Icon-Fauna-Chicken"),

                        await ManualAssetItemEntry.Create("Crow",
                            "FortniteGame/Plugins/GameFeatures/Irwin/Content/AI/Prey/Crow/Meshes/Crow_Bird",
                            "FortniteGame/Plugins/GameFeatures/Irwin/Content/Icons/T-Icon-Fauna-Crow"),

                        await ManualAssetItemEntry.Create("Frog",
                            "FortniteGame/Plugins/GameFeatures/Irwin/Content/AI/Simple/Smackie/Meshes/Smackie_Amphibian",
                            "FortniteGame/Plugins/GameFeatures/Irwin/Content/Icons/T-Icon-Fauna-Frog"),

                        await ManualAssetItemEntry.Create("Raptor",
                            "FortniteGame/Plugins/GameFeatures/Irwin/Content/AI/Predators/Robert/Meshes/Jungle_Raptor_Fauna",
                            "FortniteGame/Plugins/GameFeatures/Irwin/Content/Icons/T-Icon-Fauna-JungleRaptor"),

                        await ManualAssetItemEntry.Create("Wolf",
                            "FortniteGame/Plugins/GameFeatures/Irwin/Content/AI/Predators/Grandma/Meshes/Grandma_Mammal",
                            "FortniteGame/Plugins/GameFeatures/Irwin/Content/Icons/T-Icon-Fauna-Wolf"),

                        await ManualAssetItemEntry.Create("Llama",
                            "FortniteGame/Plugins/GameFeatures/Labrador/Content/Meshes/Labrador_Mammal",
                            "FortniteGame/Content/UI/Foundation/Textures/Icons/Athena/T-T-Icon-BR-SM-Athena-SupplyLlama-01")
                    };

                    loader.Total = entries.Length;
                    foreach (var data in entries)
                    {
                        await TaskService.RunDispatcherAsync(() => loader.Source.Add(new AssetItem(data.Mesh, data.PreviewImage, data.Name, loader.Type, "No Description.", hideRarity: true)), DispatcherPriority.Background);
                        loader.Loaded++;
                    }
                }
            },
            new(EAssetType.WeaponMod)
            {
                Classes = ["FortWeaponModItemDefinition", "FortWeaponModItemDefinitionMagazine", "FortWeaponModItemDefinitionOptic"],
                CustomLoadingHandler = async loader =>
                {
                    var weaponModTable = await CUE4ParseVM.Provider.LoadObjectAsync<UDataTable>("WeaponMods/DataTables/WeaponModOverrideData");
                    var assets = CUE4ParseVM.AssetRegistry.Where(data => loader.Classes.Contains(data.AssetClass.Text)).ToList();

                    loader.Total = assets.Count;
                    foreach (var data in assets)
                    {
                        await loader.Pause.WaitIfPaused();
                        try
                        {
                            var asset = await CUE4ParseVM.Provider.TryLoadObjectAsync(data.ObjectPath);
                            if (asset is null) continue;

                            var icon = AssetLoader.GetAssetIcon(asset) ?? asset.GetOrDefault<UTexture2D>("LargePreviewImage");
                            var tag = asset.GetOrDefault<FGameplayTag>("PluginTuningTag");
                            
                            var defaultModData = asset.GetOrDefault<FStructFallback?>("DefaultModData");
                            var mainModMeshData = defaultModData?.GetOrDefault<FStructFallback?>("MeshData");
                            var mainModMesh = mainModMeshData?.GetOrDefault<UStaticMesh?>("ModMesh");
                            
                            var overridesAdded = 0;
                            foreach (var weaponModData in weaponModTable.RowMap.Values)
                            {
                                var weaponModTag = weaponModData.GetOrDefault<FGameplayTag>("ModTag");
                                if (!tag.ToString().Equals(weaponModTag.ToString())) continue;

                                var modMeshData = weaponModData.GetOrDefault<FStructFallback>("ModMeshData");
                                var modMesh = modMeshData.GetOrDefault<UStaticMesh?>("ModMesh");
                                modMesh ??= mainModMesh;
                                if (modMesh is null) continue;
                                
                                var name = modMesh.Name;
                                if (loader.LoadedAssetsForFiltering.Contains(name)) continue;

                                await TaskService.RunDispatcherAsync(() => loader.Source.Add(new AssetItem(modMesh, icon, name, EAssetType.WeaponMod, "No Description.", hideRarity: true, useTitleCase: false)), DispatcherPriority.Background);
                                loader.LoadedAssetsForFiltering.Add(name);
                                overridesAdded++;
                            }

                            if (overridesAdded == 0 && mainModMesh is not null)
                            {
                                await TaskService.RunDispatcherAsync(() => loader.Source.Add(new AssetItem(mainModMesh, icon, mainModMesh.Name, EAssetType.WeaponMod, "No Description.", hideRarity: true, useTitleCase: false)), DispatcherPriority.Background);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Error("{0}", e);
                        }
                    }

                    loader.Loaded = loader.Total;
                }
            },
            new(EAssetType.FestivalGuitar)
            {
                Classes = new[] { "SparksGuitarItemDefinition" },
                HideRarity = true
            },
            new(EAssetType.FestivalBass)
            {
                Classes = new[] { "SparksBassItemDefinition" },
                HideRarity = true
            },
            new(EAssetType.FestivalKeytar)
            {
                Classes = new[] { "SparksKeyboardItemDefinition" },
                HideRarity = true
            },
            new(EAssetType.FestivalDrum)
            {
                Classes = new[] { "SparksDrumItemDefinition" },
                HideRarity = true
            },
            new(EAssetType.FestivalMic)
            {
                Classes = new[] { "SparksMicItemDefinition" },
                HideRarity = true
            },
        };

        SetLoader(EAssetType.Outfit);
        TaskService.Run(async () => { await CurrentLoader!.Load(); });
    }

    public void SetLoader(EAssetType assetType)
    {
        CurrentLoader = Loaders.First(x => x.Type == assetType);
        ActiveCollection = CurrentLoader.Target;
        SearchFilter = CurrentLoader.SearchFilter;
        CurrentAssetType = assetType;
        CurrentAssets.Clear();
    }

    public void ModifyFilters(string tag, bool enable)
    {
        if (!FilterPredicates.ContainsKey(tag)) return;

        if (enable)
            Filters.AddUnique(tag, FilterPredicates[tag]);
        else
            Filters.Remove(tag);

        FakeRefreshFilters();

        FilterPreviewText = Filters.Count > 0 ? Filters.Select(x => x.Key).CommaJoin(false) : "None";
    }

    [RelayCommand]
    public void Favorite()
    {
        foreach (var currentAsset in CurrentAssets) currentAsset.AssetItem.Favorite();
    }

    [RelayCommand]
    public async Task Export()
    {
        ExportChunks = 1;
        ExportProgress = 0;
        IsExporting = true;
        await ExportService.ExportAsync(CurrentAssets.ToList(), ExportType);
        IsExporting = false;
    }

    // scuffed fix to get filter to update
    private void FakeRefreshFilters()
    {
        var temp = Filters;
        Filters = null;
        Filters = temp;
    }

    private static SortExpressionComparer<AssetItem> CreateAssetSort((ESortType, bool) values)
    {
        var (type, descending) = values;

        AssetsVM.ModifyFilters("Battle Royale", type is ESortType.Season);
        
        Func<AssetItem, IComparable> sort = type switch
        {
            ESortType.Default => asset => asset.ID,
            ESortType.AZ => asset => asset.DisplayName,
            // scuffed ways to do sub-sorting within sections
            ESortType.Season => asset => asset.Season + (double) asset.Rarity * 0.01,
            ESortType.Rarity => asset => asset.Series + (int) asset.Rarity,
            _ => asset => asset.ID
        };

        return descending
            ? SortExpressionComparer<AssetItem>.Descending(sort)
            : SortExpressionComparer<AssetItem>.Ascending(sort);
    }

    private static Func<AssetItem, bool> CreateAssetFilter((string, AvaloniaDictionary<string, Predicate<AssetItem>>?) values)
    {
        var (searchFilter, filters) = values;
        if (filters is null) return _ => true;
        return asset => asset.Match(searchFilter) && filters.All(x => x.Value.Invoke(asset)) && asset.Hidden == filters.ContainsKey("Hidden Assets");
    }

    private static T? GetVehicleInfo<T>(UObject asset, params string[] names) where T : class
    {
        FStructFallback? GetMarkerDisplay(UBlueprintGeneratedClass? blueprint)
        {
            var obj = blueprint?.ClassDefaultObject.Load();
            return obj?.GetOrDefault<FStructFallback>("MarkerDisplay");
        }

        var output = asset.GetAnyOrDefault<T?>(names);
        if (output is not null) return output;

        var vehicle = asset.Get<UBlueprintGeneratedClass>("VehicleActorClass");
        output = GetMarkerDisplay(vehicle)?.GetAnyOrDefault<T?>(names);
        if (output is not null) return output;

        var vehicleSuper = vehicle.SuperStruct.Load<UBlueprintGeneratedClass>();
        output = GetMarkerDisplay(vehicleSuper)?.GetAnyOrDefault<T?>(names);
        return output;
    }
}

public partial class AssetLoader : ObservableObject
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(FinishedLoading))] private int loaded;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(FinishedLoading))] private int total;
    [ObservableProperty] private string searchFilter = string.Empty;

    public readonly EAssetType Type;
    public readonly Pauser Pause = new();
    public bool FinishedLoading => Loaded == Total;

    public readonly SourceList<AssetItem> Source = new();
    public readonly ReadOnlyObservableCollection<AssetItem> Target;
    public readonly ConcurrentBag<string> LoadedAssetsForFiltering = new();

    public string[] Classes = Array.Empty<string>();
    public string[] Filters = Array.Empty<string>();
    public bool DontLoadHiddenAssets;
    public bool HideRarity;
    public Func<AssetLoader, UObject, string, bool> HidePredicate = (_, _, _) => false;
    public Func<UObject, UTexture2D?> IconHandler = GetAssetIcon;
    public Func<UObject, FText?> DisplayNameHandler = asset => asset.GetAnyOrDefault<FText?>("DisplayName", "ItemName") ?? new FText(asset.Name);
    public Func<UObject, FText> DescriptionHandler = asset => asset.GetAnyOrDefault<FText?>("Description", "ItemDescription") ?? new FText("No description.");
    public Func<AssetLoader, Task>? CustomLoadingHandler;

    private bool Started;

    public AssetLoader(EAssetType type)
    {
        Type = type;
        Source.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Filter(AssetsVM.AssetFilter)
            .Sort(AssetsVM.AssetSort)
            .Bind(out Target)
            .Subscribe();
    }

    public async Task Load()
    {
        if (Started) return;
        Started = true;

        if (CustomLoadingHandler is not null)
        {
            await CustomLoadingHandler(this);
            return;
        }

        var assets = CUE4ParseVM.AssetRegistry.Where(data => Classes.Contains(data.AssetClass.Text)).ToList();

        var randomAsset = assets.FirstOrDefault(x => x.AssetName.Text.EndsWith("Random", StringComparison.OrdinalIgnoreCase));
        if (randomAsset is not null) assets.Remove(randomAsset);

        Total = assets.Count;
        foreach (var data in assets)
        {
            await Pause.WaitIfPaused();
            try
            {
                await LoadAsset(data);
            }
            catch (Exception e)
            {
                Log.Error("{0}", e);
            }
        }

        Loaded = Total;
    }

    private async Task LoadAsset(FAssetData data)
    {
        var asset = await CUE4ParseVM.Provider.TryLoadObjectAsync(data.ObjectPath);
        if (asset is null) return;

        var displayName = data.AssetName.Text;
        if (data.TagsAndValues.TryGetValue("DisplayName", out var displayNameRaw)) displayName = displayNameRaw.SubstringBeforeLast('"').SubstringAfterLast('"').Trim();

        await LoadAsset(asset, displayName);
    }

    private async Task LoadAsset(UObject asset, string assetDisplayName)
    {
        Loaded++;

        var isHiddenAsset = Filters.Any(y => asset.Name.Contains(y, StringComparison.OrdinalIgnoreCase)) || HidePredicate(this, asset, assetDisplayName);
        if (isHiddenAsset && DontLoadHiddenAssets) return;

        var icon = IconHandler(asset);
        if (icon is null) return;

        var displayName = DisplayNameHandler(asset)?.Text;
        if (string.IsNullOrEmpty(displayName)) displayName = asset.Name;

        var description = DescriptionHandler(asset).Text;

        await TaskService.RunDispatcherAsync(() => Source.Add(new AssetItem(asset, icon, displayName, Type, description, isHiddenAsset, HideRarity)), DispatcherPriority.Background);
    }
    
    public static UTexture2D? GetAssetIcon(UObject asset)
    {
        UTexture2D previewImage = null;
        if(asset.TryGetValue(out FInstancedStruct[] dataList, "DataList"))
        {
            foreach (var data in dataList)
            {
                if (data.NonConstStruct is not null && data.NonConstStruct.TryGetValue(out previewImage, "Icon", "LargeIcon")) break;
            }
        }

        previewImage ??= asset.GetAnyOrDefault<UTexture2D?>("Icon", "LargeIcon", "SmallPreviewImage", "LargePreviewImage");
        return previewImage;
    }
}

public class Pauser
{
    public bool IsPaused;

    public void Pause()
    {
        IsPaused = true;
    }

    public void Unpause()
    {
        IsPaused = false;
    }

    public async Task WaitIfPaused()
    {
        while (IsPaused) await Task.Delay(1);
    }
}

file class ManualAssetItemEntry
{
    public string Name;
    public UObject Mesh;
    public UTexture2D PreviewImage;

    public static async Task<ManualAssetItemEntry> Create(string name, string meshPath, string imagePath)
    {
        return new ManualAssetItemEntry
        {
            Name = name,
            Mesh = await CUE4ParseVM.Provider.LoadObjectAsync(meshPath),
            PreviewImage = await CUE4ParseVM.Provider.LoadObjectAsync<UTexture2D>(imagePath)
        };
    }
}