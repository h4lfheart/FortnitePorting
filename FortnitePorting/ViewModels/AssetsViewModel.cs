using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Engine;
using DynamicData;
using DynamicData.Binding;
using FortnitePorting.Controls;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using ReactiveUI;
using AssetItem = FortnitePorting.Controls.Assets.AssetItem;

namespace FortnitePorting.ViewModels;

public partial class AssetsViewModel : ViewModelBase
{
   
    public List<AssetLoader> Loaders;
    public AssetLoader? CurrentLoader;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsFolderOnlyExport))] private AssetItem? currentAsset;

    public bool IsFolderOnlyExport => CurrentAsset?.Type is EAssetType.LoadingScreen or EAssetType.Spray or EAssetType.MusicPack;
    [ObservableProperty] private ObservableCollection<EExportType> folderExportEnumCollection = new(new[] { EExportType.Folder});
    
    [ObservableProperty] private ReadOnlyObservableCollection<AssetItem> activeCollection;
    [ObservableProperty] private ObservableCollection<UserControl> extraOptions = new();

    [ObservableProperty] private EExportType exportType = EExportType.Blender;
    [ObservableProperty] private string searchFilter = string.Empty;
    [ObservableProperty] private ESortType sortType = ESortType.Default;
    public readonly IObservable<SortExpressionComparer<AssetItem>> AssetSort;
    public readonly IObservable<SortExpressionComparer<AssetItem>> AssetSubSort;
    public readonly IObservable<Func<AssetItem, bool>> AssetFilter;

    public AssetsViewModel()
    {
        AssetFilter = this.WhenAnyValue(x => x.SearchFilter).Select(CreateAssetFilter);
        AssetSort = this.WhenAnyValue(x => x.SortType).Select(type => SortExpressionComparer<AssetItem>.Ascending(
            type switch
            {
                ESortType.Default => asset => asset.ID,
                ESortType.AZ => asset => asset.DisplayName,
                ESortType.Season => asset => asset.Season,
                ESortType.Rarity => asset => asset.Rarity,
                ESortType.Series => asset => asset.Series,
                _ => asset => asset.ID
            }));
    }
    
    public override async Task Initialize()
    {
        Loaders = new List<AssetLoader>
        {
            new(EAssetType.Outfit)
            {
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
            },
            new(EAssetType.Backpack)
            {
                Classes = new[] { "AthenaBackpackItemDefinition" }
            },
            new(EAssetType.Pickaxe)
            {
                Classes = new[] { "AthenaPickaxeItemDefinition" },
                IconHandler = asset =>
                {
                    asset.TryGetValue(out UTexture2D? previewImage, "SmallPreviewImage", "LargePreviewImage");
                    if (asset.TryGetValue(out UObject heroDef, "WeaponDefinition"))
                    {
                        heroDef.TryGetValue(out previewImage, "SmallPreviewImage", "LargePreviewImage");
                    }

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
            new(EAssetType.Spray)
            {
                Classes = new[] { "AthenaSprayItemDefinition" }
            },
            new(EAssetType.LoadingScreen)
            {
                Classes = new[] { "FortHomebaseBannerIconItemDefinition" }
            },
            new(EAssetType.Emote)
            {
                Classes = new[] { "AthenaDanceItemDefinition" }
            },
            new(EAssetType.MusicPack)
            {
                Classes = new[] { "AthenaMusicPackItemDefinition" }
            },
            new(EAssetType.Prop)
            {
                Classes = new[] { "FortPlaysetPropItemDefinition" }
            },
            new(EAssetType.Gallery)
            {
                Classes = new[] { "FortPlaysetItemDefinition" }
            },
            new(EAssetType.Item)
            {
                Classes = new[] {"AthenaGadgetItemDefinition", "FortWeaponRangedItemDefinition", "FortWeaponMeleeItemDefinition", "FortCreativeWeaponMeleeItemDefinition", "FortCreativeWeaponRangedItemDefinition", "FortWeaponMeleeDualWieldItemDefinition" }
            },
            new(EAssetType.Trap)
            {
                Classes = new[] { "FortTrapItemDefinition" }
            },
            new(EAssetType.Vehicle)
            {
                Classes = new[] { "FortVehicleItemDefinition" },
                IconHandler = asset => GetVehicleInfo<UTexture2D>(asset, "SmallPreviewImage", "LargePreviewImage", "Icon"),
                DisplayNameHandler = asset => GetVehicleInfo<FText>(asset, "DisplayName")
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
                    
                    await Parallel.ForEachAsync(entries, async (data, _) =>
                    {
                        await Dispatcher.UIThread.InvokeAsync(() => loader.Source.Add(new AssetItem(data.Mesh, data.PreviewImage, data.Name, loader.Type)), DispatcherPriority.Background);
                    });
                }
            }
        };
        
        SetLoader(EAssetType.Outfit);
        await CurrentLoader.Load();
    }

    public void SetLoader(EAssetType assetType)
    {
        CurrentLoader = Loaders.First(x => x.Type == assetType);
        ActiveCollection = CurrentLoader.Target;
        CurrentAsset = null;
    }
    
    private static Func<AssetItem, bool> CreateAssetFilter(string filter)
    {
        return asset => asset.Match(filter);
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

public class AssetLoader
{
    public bool Started;
    public EAssetType Type;
    public Pauser Pause = new();
    
    public SourceList<AssetItem> Source = new();
    public ReadOnlyObservableCollection<AssetItem> Target;
    
    public string[] Classes = Array.Empty<string>();
    public Func<UObject, UTexture2D?> IconHandler = asset => asset.GetAnyOrDefault<UTexture2D?>("SmallPreviewImage", "LargePreviewImage");
    public Func<UObject, FText?> DisplayNameHandler = asset => asset.GetOrDefault("DisplayName", new FText(asset.Name));
    public Func<AssetLoader, Task>? CustomLoadingHandler;
    
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

        await Parallel.ForEachAsync(assets, async (data, _) =>
        {
            await Pause.WaitIfPaused();
            await LoadAsset(data);
        });
    }

    private async Task LoadAsset(FAssetData data)
    {
        var asset = await CUE4ParseVM.Provider.LoadObjectAsync(data.ObjectPath);
        await LoadAsset(asset);
    }
    
    private async Task LoadAsset(UObject asset)
    {
        var icon = IconHandler(asset);
        if (icon is null) return;

        var displayName = DisplayNameHandler(asset)?.Text;
        if (string.IsNullOrEmpty(displayName)) displayName = asset.Name;

        await Dispatcher.UIThread.InvokeAsync(() => Source.Add(new AssetItem(asset, icon, displayName, Type)), DispatcherPriority.Background);
    }
}

public class ManualAssetItemEntry
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