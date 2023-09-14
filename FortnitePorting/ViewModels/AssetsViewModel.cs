using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FortnitePorting.Controls;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using AssetItem = FortnitePorting.Controls.Assets.AssetItem;

namespace FortnitePorting.ViewModels;

public partial class AssetsViewModel : ViewModelBase
{
    [ObservableProperty] private EExportType exportType = EExportType.Blender;
    [ObservableProperty] private EAssetType currentTabType = EAssetType.Outfit;
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasSelectedAsset))] private AssetItem? currentAsset;
    public bool HasSelectedAsset => CurrentAsset is not null;

    [ObservableProperty] private ObservableCollection<AssetItem> outfits = new();
    [ObservableProperty] private ObservableCollection<AssetItem> backpacks = new();
    [ObservableProperty] private ObservableCollection<AssetItem> pickaxes = new();
    [ObservableProperty] private ObservableCollection<AssetItem> gliders = new();
    [ObservableProperty] private ObservableCollection<AssetItem> emotes = new();
    [ObservableProperty] private ObservableCollection<UserControl> extraOptions = new();

    public readonly List<AssetLoader> Loaders;

    public AssetsViewModel()
    {
        Loaders = new List<AssetLoader>
        {
            new(EAssetType.Outfit)
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
            },
            new(EAssetType.Backpack)
            {
                Target = Backpacks,
                Classes = new[] { "AthenaBackpackItemDefinition" }
            },
            new(EAssetType.Pickaxe)
            {
                Target = Pickaxes,
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
                Target = Gliders,
                Classes = new[] { "AthenaGliderItemDefinition" }
            },
            new(EAssetType.Emote)
            {
                Target = Emotes,
                Classes = new[] { "AthenaDanceItemDefinition" }
            },
        };
    }

    public AssetLoader Get(EAssetType assetType)
    {
        return Loaders.First(x => x.Type == assetType);
    }

    public override async Task Initialize()
    {
        await Loaders.First().Load();
    }
}

public class AssetLoader
{
    public bool Started;
    public Pauser Pause = new();
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

        var assets = CUE4ParseVM.AssetRegistry.Where(data => Classes.Any(className => data.AssetClass.Text.Equals(className, StringComparison.OrdinalIgnoreCase))).ToList();

        var randomAsset = assets.FirstOrDefault(x => x.AssetName.Text.Contains("Random", StringComparison.OrdinalIgnoreCase));
        if (randomAsset is not null)
        {
            assets.Remove(randomAsset);
            await LoadAsset(randomAsset, isRandom: true);
        }

        await Parallel.ForEachAsync(assets, async (data, _) =>
        {
            await Pause.WaitIfPaused();
            await LoadAsset(data);
        });
    }

    private async Task LoadAsset(FAssetData data, bool isRandom = false)
    {
        var asset = await CUE4ParseVM.Provider.LoadObjectAsync(data.ObjectPath);
        var icon = IconHandler(asset);
        if (icon is null) return;

        await Dispatcher.UIThread.InvokeAsync(() => Target.Add(new AssetItem(asset, icon, isRandom: isRandom)), DispatcherPriority.Background);
    }
}