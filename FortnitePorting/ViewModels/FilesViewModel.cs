using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.Utils;
using DynamicData;
using DynamicData.Binding;
using FortnitePorting.Models.CUE4Parse;
using FortnitePorting.Models.Files;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.Windows;
using ReactiveUI;

namespace FortnitePorting.ViewModels;

public partial class FilesViewModel : ViewModelBase
{
    [ObservableProperty] private EExportLocation _exportLocation = EExportLocation.Blender;
    
    [ObservableProperty] private string _searchFilter = string.Empty;
    [ObservableProperty] private bool _showLoadingSplash = true;

    [ObservableProperty] private FlatViewItem _selectedFlatViewItem;
    [ObservableProperty] private ReadOnlyObservableCollection<FlatViewItem> _flatViewCollection = new([]);
    
    public HashSet<string> AssetsToRemove = [];

    public SourceCache<FlatViewItem, FPackageId> AssetCache = new(item => item.Id);
    
    public override async Task Initialize()
    {
        AssetsToRemove = CUE4ParseVM.AssetRegistry
            .Select(data => CUE4ParseVM.Provider.FixPath(data.ObjectPath) + ".uasset")
            .ToHashSet();
        
        foreach (var (id, file) in CUE4ParseVM.Provider.FilesById)
        {
            var path = file.Path;
            if (IsValidFilePath(path))
            {
                AssetCache.AddOrUpdate(new FlatViewItem(id, path));
            }
        }

        var assetFilter = this
            .WhenAnyValue(viewModel => viewModel.SearchFilter)
            .Select(CreateAssetFilter);
        
        AssetCache.Connect()
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Filter(assetFilter)
            .Sort(SortExpressionComparer<FlatViewItem>.Ascending(x => x.Path))
            .Bind(out var temporaryCollection)
            .Subscribe();

        FlatViewCollection = temporaryCollection;

        await TaskService.RunAsync(() =>
        {
            while (FlatViewCollection.Count == 0) { }
            ShowLoadingSplash = false;
        });
    }
    
    [RelayCommand]
    public async Task Preview()
    {
        var asset = await CUE4ParseVM.Provider.LoadObjectAsync(FixPath(SelectedFlatViewItem.Path))!;
        var name = asset.Name;

        switch (asset)
        {
            case UVirtualTextureBuilder virtualTextureBuilder:
            {
                asset = virtualTextureBuilder.Texture.Load<UVirtualTexture2D>();
                break;
            }
            case UWorld world:
            {
                asset = world.PersistentLevel.Load<ULevel>();
                break;
            }
        }
        
        switch (asset)
        {
            case UTexture texture:
            {
                TexturePreviewWindow.Preview(name, texture);
                break;
            }
            case UStaticMesh:
            case USkeletalMesh:
            case ULevel:
            {
                ModelPreviewWindow.Preview(name, asset);
                break;
            }
            case USoundWave soundWave:
            {
                SoundPreviewWindow.Preview(soundWave);
                break;
            }
            default:
            {
                DisplayDialog("Unimplemented Previewer", 
                    $"A file previewer for \"{asset.ExportType}\" assets has not been implemented and/or will not be supported.");
                break;
            }
        }
    }
    
    private string FixPath(string path)
    {
        var outPath = path.SubstringBeforeLast(".");
        var extension = path.SubstringAfterLast(".");
        if (extension.Equals("umap"))
        {
            if (outPath.Contains("_Generated_"))
            {
                outPath += "." + path.SubstringBeforeLast("/_Generated").SubstringAfterLast("/");
            }
        }

        return outPath;
    }

    private bool IsValidFilePath(string path)
    {
        var isValidExtension = path.EndsWith(".uasset") || path.EndsWith(".umap");
        var isOptionalSegment = path.Contains(".o.");
        var isAssetRegistry = AssetsToRemove.Contains(path);
        var isEngine = path.StartsWith("Engine", StringComparison.OrdinalIgnoreCase);
        return isValidExtension && !isOptionalSegment && !isAssetRegistry && !isEngine;
    }
    
    private Func<FlatViewItem, bool> CreateAssetFilter(string filter)
    {
        return asset => MiscExtensions.Filter(asset.Path, filter);
    }
}