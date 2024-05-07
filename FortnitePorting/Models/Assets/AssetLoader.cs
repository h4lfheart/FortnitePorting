using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.Utils;
using DynamicData;
using FortnitePorting.Controls;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Services;
using ReactiveUI;
using Serilog;

namespace FortnitePorting.Models.Assets;

public partial class AssetLoader : ObservableObject
{
    public readonly EAssetType Type;
    
    public string[] ClassNames = [];
    public string PlaceholderIconPath = "FortniteGame/Content/Athena/Prototype/Textures/T_Placeholder_Generic";
    public Func<UObject, UTexture2D?> IconHandler = asset => asset.GetAnyOrDefault<UTexture2D?>("SmallPreviewImage", "LargePreviewImage");
    public Func<UObject, string> DisplayNameHandler = asset =>
    {
        var fText = asset.GetAnyOrDefault<FText?>("DisplayName", "ItemName");
        var text = fText?.Text;
        return string.IsNullOrWhiteSpace(text) ? asset.Name : text;
    };

    public readonly ReadOnlyObservableCollection<AssetItem> Filtered;
    public SourceCache<AssetItem, Guid> Source = new(item => item.Guid);

    private List<FAssetData> Assets;

    private bool BeganLoading;
    private bool IsPaused;

    public AssetLoader(EAssetType exportType)
    {
        Type = exportType;
        
        Source.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            //.Filter(AssetsVM.AssetFilter)
            .Sort(AssetsVM.AssetSort)
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
        }
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
        
        var args = new AssetItemCreationArgs
        {
            Object = asset,
            Icon = IconHandler(asset) ?? await CUE4ParseVM.Provider.TryLoadObjectAsync<UTexture2D>(PlaceholderIconPath),
            DisplayName = DisplayNameHandler(asset),
            AssetType = EAssetType.None
        };


        await TaskService.RunDispatcherAsync(() => Source.AddOrUpdate(new AssetItem(args)));
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
}