using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.Utils;
using DynamicData;
using DynamicData.Binding;
using FortnitePorting.Controls;
using FortnitePorting.Controls.Assets;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Services;
using Material.Icons;
using ReactiveUI;
using Serilog;

namespace FortnitePorting.Models.Assets;

public partial class AssetLoader : ObservableObject
{
    public readonly EExportType Type;

    public string[] ClassNames = [];
    public string[] AllowNames = [];
    public string[] HideNames = [];
    public ManuallyDefinedAsset[] ManuallyDefinedAssets = [];
    public CustomAsset[] CustomAssets = [];
    public bool LoadHiddenAssets;
    public bool HideRarity;
    public Func<AssetLoader, UObject, string, bool> HidePredicate = (loader, asset, name) => false;
    public string PlaceholderIconPath = "FortniteGame/Content/Athena/Prototype/Textures/T_Placeholder_Generic";
    public Func<UObject, UTexture2D?> IconHandler = GetIcon;
    public Func<UObject, string?> DisplayNameHandler = asset => asset.GetAnyOrDefault<FText?>("DisplayName", "ItemName")?.Text;
    public Func<UObject, string?> DescriptionHandler = asset => asset.GetAnyOrDefault<FText?>("Description", "ItemDescription")?.Text;
    public Func<UObject, FGameplayTagContainer?> GameplayTagHandler = GetGameplayTags;
    
    public readonly ReadOnlyObservableCollection<AssetItem> Filtered;
    public SourceCache<AssetItem, Guid> Source = new(item => item.Guid);
    public readonly ConcurrentBag<string> FilteredAssetBag = [];

    private List<FAssetData> Assets;

    private bool BeganLoading;
    private bool IsPaused;
    
    [ObservableProperty] private ObservableCollection<AssetInfo> _selectedAssets = [];
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(FinishedLoading))] private int _loadedAssets;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(FinishedLoading))] private int _totalAssets;
    public bool FinishedLoading => LoadedAssets == TotalAssets;
    
    public readonly IObservable<SortExpressionComparer<AssetItem>> AssetSort;
    
    [ObservableProperty] private EAssetSortType _sortType = EAssetSortType.None;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(SortIcon))] private bool _descendingSort = false;
    public MaterialIconKind SortIcon => DescendingSort ? MaterialIconKind.SortDescending : MaterialIconKind.SortAscending;
    
    public readonly IObservable<Func<AssetItem, bool>> AssetFilter;
    [ObservableProperty] private AvaloniaDictionary<string, Predicate<AssetItem>> _filters = [];
    [ObservableProperty] private string _searchFilter = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _searchAutoComplete = [];
    
    private static readonly Dictionary<string, Predicate<AssetItem>> FilterPredicates = new()
    {
        // Application
        { "Favorite", x => x.IsFavorite },
        { "Hidden Assets", x => x.CreationData.IsHidden },
        
        // Cosmetic
        { "Battle Pass", x => x.CreationData.GameplayTags.ContainsAny("BattlePass") },
        { "Item Shop", x => x.CreationData.GameplayTags.ContainsAny("ItemShop") },
        
        // Game
        { "Save The World", x => x.CreationData.GameplayTags.ContainsAny("CampaignHero", "SaveTheWorld") || x.CreationData.Object.GetPathName().Contains("SaveTheWorld", StringComparison.OrdinalIgnoreCase) },
        { "Battle Royale", x => !x.CreationData.GameplayTags.ContainsAny("CampaignHero", "SaveTheWorld") && !x.CreationData.Object.GetPathName().Contains("SaveTheWorld", StringComparison.OrdinalIgnoreCase) },
        
        // Prefab
        { "Galleries", x => x.CreationData.GameplayTags.ContainsAny("Gallery") },
        { "Prefabs", x => x.CreationData.GameplayTags.ContainsAny("Prefab") },
        { "Devices", x => x.CreationData.GameplayTags.ContainsAny("Device") },
        
        // Item
        { "Weapons", x => x.CreationData.GameplayTags.ContainsAny("Weapon") },
        { "Gadgets", x => x.CreationData.Object.ExportType.Equals("AthenaGadgetItemDefinition", StringComparison.OrdinalIgnoreCase) },
        { "Melee", x => x.CreationData.GameplayTags.ContainsAny("Melee") },
        { "Consumables", x => x.CreationData.GameplayTags.ContainsAny("Consume") },
        { "Lego", x => x.CreationData.GameplayTags.ContainsAny("Juno") },
        
    };
    
    public bool HasCosmeticFilters => CosmeticFilterTypes.Contains(Type);
    private readonly EExportType[] CosmeticFilterTypes =
    [
        EExportType.Outfit,
        EExportType.Backpack,
        EExportType.Pickaxe,
        EExportType.Glider,
        EExportType.Pet,
        EExportType.Toy,
        EExportType.Emote,
        EExportType.Emoticon,
        EExportType.Spray,
        EExportType.LoadingScreen
    ];
    
    public bool HasGameFilters => GameFilterTypes.Contains(Type);
    private readonly EExportType[] GameFilterTypes =
    [
        EExportType.Outfit,
        EExportType.Backpack,
        EExportType.Pickaxe,
        EExportType.Glider,
        EExportType.Banner,
        EExportType.LoadingScreen,
        EExportType.Item,
        EExportType.Resource,
        EExportType.Trap
    ];
    
    public bool HasPrefabFilters => Type is EExportType.Prefab;
    public bool HasItemFilters => Type is EExportType.Item;

    public AssetLoader(EExportType exportType)
    {
        Type = exportType;
        
        AssetFilter = this
            .WhenAnyValue(loader => loader.SearchFilter, loader => loader.Filters)
            .Select(CreateAssetFilter);
        
        AssetSort = this
            .WhenAnyValue(loader => loader.SortType, loader => loader.DescendingSort)
            .Select(CreateAssetSort);
        
        Source.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Filter(AssetFilter)
            .Sort(AssetSort)
            .Bind(out Filtered)
            .Subscribe();
    }

    public async Task Load()
    {
        if (BeganLoading) return;
        BeganLoading = true;
        
        Assets = CUE4ParseVM.AssetRegistry
            .Where(data => ClassNames.Contains(data.AssetClass.Text))
            .ToList();
        Assets.RemoveAll(data => data.AssetName.Text.EndsWith("Random", StringComparison.OrdinalIgnoreCase));

        if (AllowNames.Length > 0)
        {
            Assets.RemoveAll(asset => !AllowNames.Any(name => asset.PackageName.Text.Contains(name, StringComparison.OrdinalIgnoreCase)));
        }

        if (!LoadHiddenAssets)
        {
            Assets.RemoveAll(asset => HideNames.Any(name => asset.PackageName.Text.Contains(name, StringComparison.OrdinalIgnoreCase)));
        }


        TotalAssets = Assets.Count + ManuallyDefinedAssets.Length + CustomAssets.Length;
        foreach (var asset in Assets)
        {
            await WaitIfPausedAsync();
            try
            {
                await LoadAsset(asset);
            }
            catch (Exception e)
            {
                Log.Error("{0}", e);
            }

            LoadedAssets++;
        }

        foreach (var manualAsset in ManuallyDefinedAssets)
        {
            await WaitIfPausedAsync();
            try
            {
                await LoadAsset(manualAsset);
            }
            catch (Exception e)
            {
                Log.Error("{0}", e);
            }

            LoadedAssets++;
        }
        
        foreach (var customAsset in CustomAssets)
        {
            await WaitIfPausedAsync();
            try
            {
                await TaskService.RunDispatcherAsync(() =>
                {
                    SearchAutoComplete.AddUnique(customAsset.Name);
                });
                
                Source.AddOrUpdate(new AssetItem(customAsset.Name, customAsset.Description, customAsset.IconBitmap, Type, HideRarity));
            }
            catch (Exception e)
            {
                Log.Error("{0}", e);
            }

            LoadedAssets++;
        }

        LoadedAssets = TotalAssets;
    }

    private async Task LoadAsset(FAssetData data)
    {
        var asset = await CUE4ParseVM.Provider.TryLoadObjectAsync(data.ObjectPath);
        if (asset is null) return;

        /*data.TagsAndValues.TryGetValue("DisplayName", out var displayName);
        displayName ??= data.AssetName.Text;*/
        
        await LoadAsset(asset);
    }
    
    private async Task LoadAsset(UObject asset)
    {
        var displayName = DisplayNameHandler(asset);
        if (string.IsNullOrWhiteSpace(displayName)) displayName = asset.Name;
        
        var isHidden = HideNames.Any(name => asset.Name.Contains(name, StringComparison.OrdinalIgnoreCase)) || HidePredicate(this, asset, displayName);
        if (isHidden && !LoadHiddenAssets) return;
        
        var icon = IconHandler(asset) ?? await CUE4ParseVM.Provider.TryLoadObjectAsync<UTexture2D>(PlaceholderIconPath);
        if (icon is null) return;
        
        await TaskService.RunDispatcherAsync(() =>
        {
            SearchAutoComplete.AddUnique(displayName);
        });
        
        var args = new AssetItemCreationArgs
        {
            Object = asset,
            Icon = icon,
            DisplayName = displayName,
            Description = DescriptionHandler(asset) ?? "No Description.",
            ExportType = Type,
            IsHidden = isHidden,
            HideRarity = HideRarity,
            GameplayTags = GameplayTagHandler(asset)
        };

        Source.AddOrUpdate(new AssetItem(args));
    }
    
    private async Task LoadAsset(ManuallyDefinedAsset manualAsset)
    {
        var asset = await CUE4ParseVM.Provider.TryLoadObjectAsync(manualAsset.AssetPath);
        if (asset is null) return;

        var displayName = manualAsset.Name;
            
        var icon = await CUE4ParseVM.Provider.TryLoadObjectAsync<UTexture2D>(manualAsset.IconPath) ?? await CUE4ParseVM.Provider.TryLoadObjectAsync<UTexture2D>(PlaceholderIconPath);
        if (icon is null) return;
        
        await TaskService.RunDispatcherAsync(() =>
        {
            SearchAutoComplete.AddUnique(displayName);
        });
        
        var args = new AssetItemCreationArgs
        {
            Object = asset,
            Icon = icon,
            DisplayName = displayName,
            Description = manualAsset.Description,
            ExportType = Type,
            HideRarity = HideRarity,
            GameplayTags = GameplayTagHandler(asset)
        };

        Source.AddOrUpdate(new AssetItem(args));
    }
    
    public static UTexture2D? GetIcon(UObject asset)
    {
        return asset.GetDataListItem<UTexture2D>("Icon", "LargeIcon")
               ?? asset.GetAnyOrDefault<UTexture2D?>("Icon", "SmallPreviewImage", "LargeIcon", "LargePreviewImage");
    }
    
    public static FGameplayTagContainer? GetGameplayTags(UObject asset)
    {
        return asset.GetDataListItem<FGameplayTagContainer?>("Tags")
               ?? asset.GetOrDefault<FGameplayTagContainer?>("GameplayTags");
    }
    
    public void ModifyFilters(string tag, bool enable)
    {
        if (!FilterPredicates.TryGetValue(tag, out var predicate)) return;

        if (enable)
            Filters.AddUnique(tag, predicate);
        else
            Filters.Remove(tag);

        FakeRefreshFilters();
    }
    
    public void Pause()
    {
        IsPaused = true;
    }

    public void Unpause()
    {
        IsPaused = false;
    }

    private async Task WaitIfPausedAsync()
    {
        while (IsPaused) await Task.Delay(1);
    }
    
    private static Func<AssetItem, bool> CreateAssetFilter((string, AvaloniaDictionary<string, Predicate<AssetItem>>) values)
    {
        var (searchFilter, filters) = values;
        
        return asset => asset.Match(searchFilter) && filters.All(x => x.Value.Invoke(asset)) && asset.CreationData.IsHidden == filters.ContainsKey("Hidden Assets");
    }

    private static SortExpressionComparer<AssetItem> CreateAssetSort((EAssetSortType, bool) values)
    {
        var (type, descending) = values;
        Func<AssetItem, IComparable> sort = type switch
        {
            EAssetSortType.AZ => asset => asset.CreationData.DisplayName,
            EAssetSortType.Season => asset => asset.Season + (double) asset.Rarity * 0.01,
            EAssetSortType.Rarity => asset => asset.Series?.DisplayName.Text + (int) asset.Rarity,
            _ => asset => asset.CreationData.Object?.Name ?? string.Empty
        };

        return descending
            ? SortExpressionComparer<AssetItem>.Descending(sort)
            : SortExpressionComparer<AssetItem>.Ascending(sort);
    }
    
    // scuffed fix to get filter to update
    private void FakeRefreshFilters()
    {
        var temp = Filters;
        Filters = [];
        Filters = temp;
    }
    
}