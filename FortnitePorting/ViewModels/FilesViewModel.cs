using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.Utils;
using DynamicData;
using DynamicData.Binding;
using FortnitePorting.Application;
using FortnitePorting.Export;
using FortnitePorting.Export.Models;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Assets;
using FortnitePorting.Models.Files;
using FortnitePorting.Models.Leaderboard;
using FortnitePorting.Models.Unreal;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.Windows;
using Newtonsoft.Json;
using ReactiveUI;

namespace FortnitePorting.ViewModels;

public partial class FilesViewModel : ViewModelBase
{
    [ObservableProperty] private EExportLocation _exportLocation = EExportLocation.Blender;
    
    [ObservableProperty] private string _searchFilter = string.Empty;
    [ObservableProperty] private bool _useRegex = false;
    [ObservableProperty] private bool _showLoadingSplash = true;

    [ObservableProperty] private List<FlatViewItem> _selectedFlatViewItems = [];
    [ObservableProperty] private ReadOnlyObservableCollection<FlatViewItem> _flatViewCollection = new([]);

    public SourceCache<FlatViewItem, int> AssetCache = new(item => item.Id);
    
    public override async Task Initialize()
    {
        foreach (var (_, file) in CUE4ParseVM.Provider.Files)
        {
            var path = file.Path;
            if (IsValidFilePath(path))
            {
                AssetCache.AddOrUpdate(new FlatViewItem(path.GetHashCode(), path));
            }
        }

        var assetFilter = this
            .WhenAnyValue(viewModel => viewModel.SearchFilter, viewmodel => viewmodel.UseRegex)
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
    public async Task Properties()
    {
        var selectedItem = SelectedFlatViewItems.FirstOrDefault();
        if (selectedItem is null) return;
        
        var assets = await CUE4ParseVM.Provider.LoadAllObjectsAsync(Exporter.FixPath(selectedItem.Path));
        var json = JsonConvert.SerializeObject(assets, Formatting.Indented);
        PropertiesPreviewWindow.Preview(selectedItem.Path.SubstringAfterLast("/").SubstringBefore("."), json);
    }
    
    
    [RelayCommand]
    public async Task Preview()
    {
        var selectedItem = SelectedFlatViewItems.FirstOrDefault();
        if (selectedItem is null) return;
        
        var asset = await CUE4ParseVM.Provider.LoadObjectAsync(Exporter.FixPath(selectedItem.Path));
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
                ModelPreviewWindow.Preview([asset]);
                break;
            }
            case USoundWave soundWave:
            {
                SoundPreviewWindow.Preview(soundWave);
                break;
            }
            case USoundCue soundCue:
            {
                var sounds = soundCue.HandleSoundTree();
                var soundWaveLazy = sounds.MaxBy(sound => sound.Time)?.SoundWave;
                if (soundWaveLazy?.Load<USoundWave>() is not { } soundWave) break;
                
                SoundPreviewWindow.Preview(soundWave);
                break;
            }
            default:
            {
                AppWM.Dialog("Unimplemented Previewer", 
                    $"A file previewer for \"{asset.ExportType}\" assets has not been implemented and/or will not be supported.");
                break;
            }
        }
    }
    
    [RelayCommand]
    public async Task Export()
    {
        var exports = new List<KeyValuePair<UObject, EExportType>>();
        foreach (var item in SelectedFlatViewItems)
        {
            var basePath = Exporter.FixPath(item.Path);
            var asset = await CUE4ParseVM.Provider.TryLoadObjectAsync(basePath);
            asset ??= await CUE4ParseVM.Provider.TryLoadObjectAsync($"{basePath}.{basePath.SubstringAfterLast("/")}_C");
            if (asset is null) continue;
            
            switch (asset)
            {
                case UVirtualTextureBuilder virtualTextureBuilder:
                {
                    asset = virtualTextureBuilder.Texture.Load<UVirtualTexture2D>();
                    break;
                }
            }

            var exportType = Exporter.DetermineExportType(asset);
            if (exportType is EExportType.None)
            {
                AppWM.Dialog("Unimplemented Exporter", 
                    $"A file exporter for \"{asset.ExportType}\" assets has not been implemented and/or will not be supported.");
            }
            else
            {
                exports.Add(new KeyValuePair<UObject, EExportType>(asset, exportType));
            }
        }
        
        if (exports.Count == 0) return;
        
        await Exporter.Export(exports, AppSettings.Current.CreateExportMeta(ExportLocation));

        if (AppSettings.Current.Online.UseIntegration)
        {
            var sendExports = exports.Select(export =>
            {
                var (asset, type) = export;
                return new PersonalExport(asset.GetPathName());
            });
            
            await ApiVM.FortnitePorting.PostExportsAsync(sendExports);
        }
    }

    private bool IsValidFilePath(string path)
    {
        var isValidExtension = path.EndsWith(".uasset") || path.EndsWith(".umap");
        var isOptionalSegment = path.Contains(".o.");
        var isEngine = path.StartsWith("Engine", StringComparison.OrdinalIgnoreCase);
        return isValidExtension && !isOptionalSegment && !isEngine;
    }
    
    private Func<FlatViewItem, bool> CreateAssetFilter((string, bool) items)
    {
        var (filter, useRegex) = items;
        if (UseRegex)
        {
            return asset => Regex.IsMatch(asset.Path, filter);
        }

        return asset => MiscExtensions.Filter(asset.Path, filter);
    }
}