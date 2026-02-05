using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.GameplayTags;
using DynamicData;
using DynamicData.Binding;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Assets.Asset;
using FortnitePorting.Models.Assets.Base;
using FortnitePorting.Models.Assets.Custom;
using FortnitePorting.Models.Assets.Filters;
using Material.Icons;
using ReactiveUI;
using Serilog;

namespace FortnitePorting.Models.Assets.Loading;

public partial class AssetLoader : ObservableObject
{
    public readonly EExportType Type;

    public string[] ClassNames = [];
    public string[] AllowNames = [];
    public string[] HideNames = [];
    public string[] DisallowedNames = [];
    public Lazy<ManuallyDefinedAsset[]> ManuallyDefinedAssets = new([]);
    public CustomAsset[] CustomAssets = [];
    public bool LoadHiddenAssets;
    public bool HideRarity;
    public Func<AssetLoader, UObject, string, bool> HidePredicate = (loader, asset, name) => false;
    public Action<AssetLoader, UObject, string> AddStyleHandler = (loader, asset, name) => {};
    public string PlaceholderIconPath = "FortniteGame/Content/Athena/Prototype/Textures/T_Placeholder_Generic";
    public Func<UObject, UTexture2D?> IconHandler = GetIcon;
    public Func<UObject, string?> DisplayNameHandler = asset => asset.GetAnyOrDefault<FText?>("DisplayName", "ItemName")?.Text;
    public Func<UObject, string?> DescriptionHandler = asset => asset.GetAnyOrDefault<FText?>("Description", "ItemDescription")?.Text;
    public Func<UObject, FGameplayTagContainer?> GameplayTagHandler = GetGameplayTags;
    
    public readonly ConcurrentBag<BaseAssetItem> AssetBag = [];
    public readonly ReadOnlyObservableCollection<BaseAssetItem> Filtered;
    public SourceCache<BaseAssetItem, Guid> Source = new(asset => asset.Id);
    public readonly ConcurrentBag<string> FilteredAssetBag = [];
    public readonly ConcurrentDictionary<string, ConcurrentBag<string>> StyleDictionary = [];

    private List<FAssetData> Assets;

    private bool BeganLoading;
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(PauseIcon))] private bool _isPaused = false;
    public MaterialIconKind PauseIcon => IsPaused ? MaterialIconKind.Play : MaterialIconKind.Pause;
    
    [ObservableProperty] private ObservableCollection<BaseAssetInfo> _selectedAssetInfos = [];
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(LoadingPercentageText))] private int _loadedAssets;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(LoadingPercentageText))] private int _totalAssets = int.MaxValue;
    [ObservableProperty] private bool _finishedLoading = false;
    
    public string LoadingStatus => $"Loading {Type.Description}";
    public string LoadingPercentageText => $"{(LoadedAssets == 0 && TotalAssets == 0 ? 0 : LoadedAssets * 100f / TotalAssets):N0}%";
    
    
    public readonly IObservable<SortExpressionComparer<BaseAssetItem>> AssetSort;
    [ObservableProperty] private EAssetSortType _sortType = EAssetSortType.None;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(SortIcon))] private bool _descendingSort = false;
    public MaterialIconKind SortIcon => DescendingSort ? MaterialIconKind.SortDescending : MaterialIconKind.SortAscending;
    
    public readonly IObservable<Func<BaseAssetItem, bool>> AssetFilter;
    [ObservableProperty] private string _searchFilter = string.Empty;
    [ObservableProperty] private bool _useRegex = false;
    [ObservableProperty] private ObservableCollection<string> _searchAutoComplete = [];
    
    private readonly SemaphoreSlim _pauseSemaphore = new(1, 1);
    
    [ObservableProperty] private ObservableCollection<FilterItem> _activeFilters = [];
    public List<FilterCategory> FilterCategories { get; } =
    [
        new("APPLICATION")
        {
            Filters = 
            [
                new FilterItem("Favorites", asset => asset.IsFavorite),
                new FilterItem("Hidden Items", asset => asset.CreationData.IsHidden)
            ]
        },
        new("COSMETIC")
        {
            Filters = 
            [
                new FilterItem("Battle Pass", asset => asset.CreationData.GameplayTags.ContainsAny("BattlePass")),
                new FilterItem("Item Shop", asset => asset.CreationData.GameplayTags.ContainsAny("ItemShop"))
            ],
            AllowedTypes = 
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
            ]
        },
        new("EMOTE")
        {
            Filters = 
            [
                new FilterItem("Synced", asset => asset.CreationData.GameplayTags.ContainsAny("Synced")),
                new FilterItem("Traversal", asset => asset.CreationData.GameplayTags.ContainsAny("Traversal"))
            ],
            AllowedTypes = 
            [
                EExportType.Emote
            ]
        },
        new("GAME")
        {
            Filters = 
            [
                new FilterItem("Battle Royale", asset => !(asset.CreationData.GameplayTags.ContainsAny("CampaignHero", "SaveTheWorld") 
                                                         || asset.CreationData.Object.GetPathName().Contains("SaveTheWorld", StringComparison.OrdinalIgnoreCase))),
                new FilterItem("Save The World", asset => asset.CreationData.GameplayTags.ContainsAny("CampaignHero", "SaveTheWorld") 
                                                           || asset.CreationData.Object.GetPathName().Contains("SaveTheWorld", StringComparison.OrdinalIgnoreCase))
            ],
            AllowedTypes = 
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
            ]
        },
        new("CREATIVE")
        {
            Filters = 
            [
                new FilterItem("Galleries", asset => asset.CreationData.GameplayTags.ContainsAny("Gallery")),
                new FilterItem("Prefabs", asset => asset.CreationData.GameplayTags.ContainsAny("Prefab")),
                new FilterItem("Devices", asset => asset.CreationData.GameplayTags.ContainsAny("Device")),
            ],
            AllowedTypes = 
            [
               EExportType.Prefab
            ]
        },
        new("ITEM")
        {
            Filters = 
            [
                new FilterItem("Weapons", asset => asset.CreationData.GameplayTags.ContainsAny("Weapon")),
                new FilterItem("Gadgets", asset => asset.CreationData.Object.ExportType.Equals("AthenaGadgetItemDefinition", StringComparison.OrdinalIgnoreCase)),
                new FilterItem("Melee", asset => asset.CreationData.GameplayTags.ContainsAny("Melee")),
                new FilterItem("Consumables", asset => asset.CreationData.GameplayTags.ContainsAny("Consume")),
                new FilterItem("Lego", asset => asset.CreationData.GameplayTags.ContainsAny("Juno")),
            ],
            AllowedTypes = 
            [
                EExportType.Item
            ]
        }
    ];

    public AssetLoader(EExportType exportType)
    {
        Type = exportType;
        
        AssetFilter = this
            .WhenAnyValue(loader => loader.SearchFilter, loader => loader.ActiveFilters, loader => loader.UseRegex)
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
        
        Assets = UEParse.AssetRegistry
            .Where(data => ClassNames.Contains(data.AssetClass.Text))
            .ToList();
        Assets.RemoveAll(data => data.AssetName.Text.EndsWith("Random", StringComparison.OrdinalIgnoreCase));

        Assets.RemoveAll(asset => DisallowedNames.Any(name => asset.PackageName.Text.Contains(name, StringComparison.OrdinalIgnoreCase)));
        
        if (AllowNames.Length > 0)
        {
            Assets.RemoveAll(asset => !AllowNames.Any(name => asset.PackageName.Text.Contains(name, StringComparison.OrdinalIgnoreCase)));
        }

        if (!LoadHiddenAssets)
        {
            Assets.RemoveAll(asset => HideNames.Any(name => asset.PackageName.Text.Contains(name, StringComparison.OrdinalIgnoreCase)) || asset.PackageName.Text.Contains("Placeholder", StringComparison.OrdinalIgnoreCase));
        }


        var manuallyDefinedAssets = ManuallyDefinedAssets.Value;
        TotalAssets = Assets.Count + manuallyDefinedAssets.Length + CustomAssets.Length;
        
        await Parallel.ForEachAsync(Assets, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, 
            async (asset, ct) =>
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
            });
      

        foreach (var manualAsset in manuallyDefinedAssets)
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
                await LoadAsset(customAsset);
            }
            catch (Exception e)
            {
                Log.Error("{0}", e);
            }

            LoadedAssets++;
        }
        
        Source.AddOrUpdate(AssetBag);
        AssetBag.Clear();
        LoadedAssets = TotalAssets;
        FinishedLoading = true;
    }

    private async Task LoadAsset(FAssetData data)
    {
        var asset = await UEParse.Provider.SafeLoadPackageObjectAsync(data.ObjectPath);
        if (asset is null) return;

        /*data.TagsAndValues.TryGetValue("DisplayName", out var displayName);
        displayName ??= data.AssetName.Text;*/
        
        await LoadAsset(asset);
    }
    
    private async Task LoadAsset(UObject asset)
    {
        var displayName = DisplayNameHandler(asset);
        if (string.IsNullOrWhiteSpace(displayName)) displayName = asset.Name;
        
        AddStyleHandler(this, asset, displayName);
        
        var isHidden = HideNames.Any(name => asset.Name.Contains(name, StringComparison.OrdinalIgnoreCase)) || HidePredicate(this, asset, displayName);
        if (isHidden && !LoadHiddenAssets) return;
        
        var icon = IconHandler(asset) ?? await UEParse.Provider.SafeLoadPackageObjectAsync<UTexture2D>(PlaceholderIconPath);
        if (icon is null) return;
        
        var args = new AssetItemCreationArgs
        {
            Object = asset,
            Icon = icon,
            ID = asset.Name,
            DisplayName = displayName,
            Description = DescriptionHandler(asset) ?? "No Description.",
            ExportType = Type,
            IsHidden = isHidden,
            HideRarity = HideRarity,
            GameplayTags = GameplayTagHandler(asset)
        };

        AssetBag.Add(new AssetItem(args));
    }
    
    private async Task LoadAsset(ManuallyDefinedAsset manualAsset)
    {
        var asset = await UEParse.Provider.SafeLoadPackageObjectAsync(manualAsset.AssetPath);
        if (asset is null) return;

        var displayName = manualAsset.Name;
            
        var icon = await UEParse.Provider.SafeLoadPackageObjectAsync<UTexture2D>(manualAsset.IconPath) ?? await UEParse.Provider.SafeLoadPackageObjectAsync<UTexture2D>(PlaceholderIconPath);
        if (icon is null) return;
        
        var args = new AssetItemCreationArgs
        {
            Object = asset,
            Icon = icon,
            ID = asset.Name,
            DisplayName = displayName,
            Description = manualAsset.Description,
            ExportType = Type,
            HideRarity = HideRarity,
            GameplayTags = GameplayTagHandler(asset)
        };

        AssetBag.Add(new AssetItem(args));
    }
    
    private async Task LoadAsset(CustomAsset customAsset)
    {
        AssetBag.Add(new CustomAssetItem(customAsset, Type));
    }
    
    public static UTexture2D? GetIcon(UObject asset)
    {
        return asset.GetDataListItem<UTexture2D?>( "Icon", "LargeIcon")
               ?? asset.GetAnyOrDefault<UTexture2D?>("Icon", "SmallPreviewImage", "LargeIcon", "LargePreviewImage");
    }
    
    public static FGameplayTagContainer? GetGameplayTags(UObject asset)
    {
        return asset.GetDataListItem<FGameplayTagContainer?>("Tags")
               ?? asset.GetOrDefault<FGameplayTagContainer?>("GameplayTags");
    }
    
    public void UpdateFilterVisibility()
    {
        foreach (var category in FilterCategories)
        {
            category.IsVisible = category.AllowedTypes.Count == 0 || category.AllowedTypes.Contains(Type);
        }
    }

    public void UpdateFilters(FilterItem item, bool add)
    {
        if (add)
            ActiveFilters.Add(item);
        else
            ActiveFilters.Remove(item);
        
        FakeRefreshFilters();
    }
    
    
    private async Task WaitIfPausedAsync()
    {
        if (IsPaused)
        {
            await _pauseSemaphore.WaitAsync();
            _pauseSemaphore.Release();
        }
    }

    public void Pause()
    {
        if (!IsPaused)
        {
            _pauseSemaphore.Wait(); // Acquire the semaphore
            IsPaused = true;
        }
    }

    public void Unpause()
    {
        if (IsPaused)
        {
            IsPaused = false;
            _pauseSemaphore.Release(); // Release waiting tasks
        }
    }
    
    private static Func<BaseAssetItem, bool> CreateAssetFilter((string, ObservableCollection<FilterItem>, bool) values)
    {
        var (searchFilter, filters, useRegex) = values;
        return asset =>
        {
            if (asset is AssetItem assetItem)
            {
                return assetItem.Match(searchFilter, useRegex) && filters.All(x => x.Predicate.Invoke(assetItem)) 
                                                     && assetItem.CreationData.IsHidden == filters.Any(filter => filter.Title.Equals("Hidden Items"));
            }

            return asset.Match(searchFilter, useRegex);
        };
        
    }

    private static SortExpressionComparer<BaseAssetItem> CreateAssetSort((EAssetSortType, bool) values)
    {
        var (type, descending) = values;
        Func<BaseAssetItem, IComparable> sort = type switch
        {
            EAssetSortType.AZ => asset => asset.CreationData.DisplayName,
            EAssetSortType.Season => asset => asset is AssetItem assetItem ? assetItem.Season + (double) assetItem.Rarity * 0.01 : 0,
            EAssetSortType.Rarity => asset => asset is AssetItem assetItem ? assetItem.Series?.DisplayName.Text + (int) assetItem.Rarity : asset.CreationData.DisplayName,
            _ => asset => asset is AssetItem assetItem ? assetItem.CreationData.Object?.Name ?? string.Empty : asset.CreationData.DisplayName
        };

        return descending
            ? SortExpressionComparer<BaseAssetItem>.Descending(sort)
            : SortExpressionComparer<BaseAssetItem>.Ascending(sort);
    }
    
    // scuffed fix to get filter to update
    private void FakeRefreshFilters()
    {
        var temp = ActiveFilters;
        ActiveFilters = [];
        ActiveFilters = temp;
    }
    
}