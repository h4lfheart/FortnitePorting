using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.Utils;
using DynamicData;
using DynamicData.Binding;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Exporting;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.Assets;
using FortnitePorting.Models.Files;
using FortnitePorting.Models.Fortnite;
using FortnitePorting.Models.Leaderboard;
using FortnitePorting.Models.Unreal;
using FortnitePorting.Models.Unreal.Material;
using FortnitePorting.OnlineServices.Packet;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Services;
using FortnitePorting.Windows;
using Newtonsoft.Json;
using ReactiveUI;
using Serilog;

namespace FortnitePorting.ViewModels;

public partial class FilesViewModel() : ViewModelBase
{
    [ObservableProperty] private CUE4ParseService _CUE4Parse;
    
    public FilesViewModel(CUE4ParseService cue4Parse) : this()
    {
        CUE4Parse = cue4Parse;
    }
    
    [ObservableProperty] private EExportLocation _exportLocation = EExportLocation.Blender;

    [ObservableProperty] private string _actualSearchText;

    // fixes freezes when using ObservableProperty
    private string _searchFilter = string.Empty;
    
    public string SearchFilter
    {
        get => _searchFilter;
        set
        {
            _searchFilter = value;
            OnPropertyChanged();
        }
    }
    [ObservableProperty] private bool _useRegex = false;
    [ObservableProperty] private bool _showLoadingSplash = true;

    [ObservableProperty] private ObservableCollection<FileGameFilter> _gameNames = 
    [
        new("FortniteGame"),
        new("Engine"),
    ];

    [ObservableProperty] private ObservableCollection<string> _selectedGameNames = [];

    [ObservableProperty] private List<FlatItem> _selectedFlatViewItems = [];
    [ObservableProperty] private ReadOnlyObservableCollection<FlatItem> _flatViewCollection = new([]);

    [ObservableProperty] private TreeItem _selectedTreeItem;
    [ObservableProperty] private ObservableCollection<TreeItem> _treeViewCollection = new([]);
    
    public readonly SourceCache<FlatItem, string> FlatViewAssetCache = new(item => item.Path);
    
    private Dictionary<string, TreeItem> _treeViewBuildHierarchy = [];
    
    public override async Task Initialize()
    {
        if (ChatVM.Permissions.HasFlag(EPermissions.LoadPluginFiles))
        {
            foreach (var mountedVfs in CUE4Parse.Provider.MountedVfs)
            {
                if (mountedVfs is not IoStoreReader { Name: "plugin.utoc" } ioStoreReader) continue;

                var gameFeatureDataFile = ioStoreReader.Files.FirstOrDefault(file => file.Key.EndsWith("GameFeatureData.uasset", StringComparison.OrdinalIgnoreCase));
                if (gameFeatureDataFile.Value is null) continue;

                var gameFeatureData = await CUE4Parse.Provider.SafeLoadPackageObjectAsync<UFortGameFeatureData>(gameFeatureDataFile.Value.PathWithoutExtension);

                if (gameFeatureData?.ExperienceData?.DefaultMap is not { } defaultMapPath) continue;

                var defaultMap = await defaultMapPath.LoadAsync();
                GameNames.Add(new FileGameFilter(defaultMap.Name, defaultMapPath.AssetPathName.Text[1..].SubstringBefore("/")));
            }
        }

        foreach (var gameName in GameNames)
        {
            SelectedGameNames.AddUnique(gameName.SearchName);
        }
        
        foreach (var (_, file) in CUE4Parse.Provider.Files)
        {
            var path = file.Path;
            if (!IsValidFilePath(path)) continue;
            
            FlatViewAssetCache.AddOrUpdate(new FlatItem(path));

            var folderNames = path.Split("/", StringSplitOptions.RemoveEmptyEntries);
            var children = _treeViewBuildHierarchy;
            for (var i = 0; i < folderNames.Length; i++)
            {
                var folderName = folderNames[i];
                if (!children.TryGetValue(folderName, out var foundNode))
                {
                    var isFile = i == folderNames.Length - 1;
                    foundNode = new TreeItem(folderName, isFile ? ENodeType.File : ENodeType.Folder,
                        isFile ? path : string.Empty);
                    children.Add(folderName, foundNode);
                }

                children = foundNode.Children;
            }
        }

        void SortChildren(ref Dictionary<string, TreeItem> items)
        {
            items = items
                .OrderBy(item => item.Value.Name)
                .OrderByDescending(item => item.Value.Type == ENodeType.Folder)
                .ToDictionary();

            foreach (var child in items)
            {
                var tempReference = child.Value.Children;
                SortChildren(ref tempReference);
                child.Value.Children = tempReference;
            }
        }
        
        SortChildren(ref _treeViewBuildHierarchy);
        
        var assetFilter = this
            .WhenAnyValue(viewModel => viewModel.SearchFilter, 
                viewmodel => viewmodel.UseRegex,
                viewmodel => viewmodel.SelectedGameNames)
            .Select(CreateAssetFilter);
        
        FlatViewAssetCache.Connect()
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Filter(assetFilter)
            .Sort(SortExpressionComparer<FlatItem>.Ascending(x => x.Path))
            .Bind(out var flatCollection)
            .Subscribe();

        FlatViewCollection = flatCollection;

        await TaskService.RunAsync(() =>
        {
            while (FlatViewCollection.Count == 0) { }
            TreeViewCollection = [.._treeViewBuildHierarchy.Values];
            ShowLoadingSplash = false;
        });
    }

    public override async Task OnViewOpened()
    {
        DiscordService.Update("Browsing Files", "Files");
    }

    public void ClearSearchFilter()
    {
        ActualSearchText = string.Empty;
        SearchFilter = string.Empty;
    }

    public void FlatViewJumpTo(string directory)
    {
        var foundItem = FlatViewAssetCache.Lookup(directory);
        if (!foundItem.HasValue) return;

        SelectedFlatViewItems = [foundItem.Value];
    }
    
    public void TreeViewJumpTo(string directory)
    {
        var i = 0;
        var folders = directory.Split('/');
        var children = _treeViewBuildHierarchy; // start at root
        while (true)
        {
            foreach (var (_, item) in children)
            {
                if (!item.Name.Equals(folders[i], StringComparison.OrdinalIgnoreCase))
                    continue;

                if (item.Type == ENodeType.File)
                {
                    SelectedTreeItem = item;
                    return;
                }

                item.Expanded = true;
                children = item.Children;
                break;
            }

            i++;
            
            if (children.Count == 0) break;
        }
    }

    [RelayCommand]
    public async Task Properties()
    {
        var selectedItem = SelectedFlatViewItems.FirstOrDefault();
        if (selectedItem is null) return;
        
        var assets = await CUE4Parse.Provider.LoadAllObjectsAsync(Exporter.FixPath(selectedItem.Path));
        var json = JsonConvert.SerializeObject(assets, Formatting.Indented);
        PropertiesPreviewWindow.Preview(selectedItem.Path.SubstringAfterLast("/").SubstringBefore("."), json);
    }
    
    [RelayCommand]
    public async Task Preview()
    {
        var selectedItem = SelectedFlatViewItems.FirstOrDefault();
        if (selectedItem is null) return;
        
        var basePath = Exporter.FixPath(selectedItem.Path);
            
        UObject? asset = null;
        if (selectedItem.Path.EndsWith(".umap"))
        {
            var package = await CUE4Parse.Provider.LoadPackageAsync(basePath);
            asset = package.GetExports().OfType<UWorld>().FirstOrDefault();
        }
        else
        {
            asset = await CUE4Parse.Provider.SafeLoadPackageObjectAsync(basePath);
            asset ??= await CUE4Parse.Provider.SafeLoadPackageObjectAsync($"{basePath}.{basePath.SubstringAfterLast("/")}_C");
        }
            
        if (asset is null) return;

        await PreviewAsset(asset);
    }

    public async Task PreviewAsset(UObject asset)
    {
        var name = asset.Name;

        switch (asset)
        {
            case UVirtualTextureBuilder virtualTextureBuilder:
            {
                asset = virtualTextureBuilder.Texture.Load<UVirtualTexture2D>();
                break;
            }
            
            case UPaperSprite paperSprite:
            {
                asset = paperSprite.BakedSourceTexture.Load<UTexture2D>();
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
            case UMaterial:
            case UMaterialFunction:
            {
                if (!CUE4Parse.Provider.MountedVfs.Any(vfs => vfs.Name.Contains(".o.")))
                {
                    Info.Message("Material Preview", "Material node-tree data cannot be loaded because UEFN is not installed.", closeTime: 5, severity: InfoBarSeverity.Error);
                    break;
                }
                
                MaterialPreviewWindow.Preview(asset);
                break;
            }
            case UMaterialInstanceConstant instance:
            {
                var dialog = new ContentDialog
                {
                    Title = $"Preview {instance.Name}",
                    Content = "What asset type would you like to preview?",
                    CloseButtonText = "Cancel",
                    SecondaryButtonText = "Material Instance",
                    SecondaryButtonCommand = new RelayCommand(async () =>
                    {
                        await Properties();
                    }),
                    PrimaryButtonText = "Parent Material",
                    PrimaryButtonCommand = new RelayCommand(() =>
                    {
                        UUnrealMaterial? parentMaterial = instance;
                        while (parentMaterial is UMaterialInstanceConstant parentMaterialInstance)
                        {
                            parentMaterial = parentMaterialInstance.Parent;
                        }
                        
                        if (parentMaterial is not null)
                            MaterialPreviewWindow.Preview(parentMaterial);
                    })
                };

                await dialog.ShowAsync();
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
                Info.Message("Unimplemented Previewer",
                    $"A file previewer for \"{asset.ExportType}\" assets has not been implemented and/or will not be supported.");
                    
                await Properties();
                break;
            }
        }
    }
    
    [RelayCommand]
    public async Task Export()
    {
        var exports = new List<KeyValuePair<UObject, EExportType>>();
        var unsupportedExportTypes = new HashSet<string>();
        foreach (var item in SelectedFlatViewItems)
        {
            var basePath = Exporter.FixPath(item.Path);
            
            UObject? asset = null;
            if (item.Path.EndsWith(".umap"))
            {
                asset = await CUE4Parse.Provider.SafeLoadPackageObjectAsync(basePath);
                if (asset is not UWorld)
                {
                    var package = await CUE4Parse.Provider.LoadPackageAsync(basePath);
                    asset = package.GetExports().OfType<UWorld>().FirstOrDefault();
                }
            }
            else
            {
                asset = await CUE4Parse.Provider.SafeLoadPackageObjectAsync(basePath);
                asset ??= await CUE4Parse.Provider.SafeLoadPackageObjectAsync($"{basePath}.{basePath.SubstringAfterLast("/")}_C");
            }
            
            if (asset is null) continue;
            
            switch (asset)
            {
                case UVirtualTextureBuilder virtualTextureBuilder:
                {
                    asset = virtualTextureBuilder.Texture.Load<UVirtualTexture2D>();
                    break;
                }
                case UPaperSprite paperSprite:
                {
                    asset = paperSprite.BakedSourceTexture.Load<UTexture2D>();
                    break;
                }
            }

            var exportType = Exporter.DetermineExportType(asset);
            if (exportType is EExportType.None)
            {
                unsupportedExportTypes.Add(asset.ExportType);
                continue;
            }

            exports.Add(new KeyValuePair<UObject, EExportType>(asset, exportType));
        }

        if (exports.Count == 0)
        {
            Info.Message("Unimplemented Exporter", $"Assets with these types could not be exported: {unsupportedExportTypes.CommaJoin()}.");
            return;
        }

        var meta = AppSettings.ExportSettings.CreateExportMeta(ExportLocation);
        meta.WorldFlags = EWorldFlags.Actors | EWorldFlags.Landscape | EWorldFlags.WorldPartitionGrids | EWorldFlags.HLODs;
        if (meta.Settings.ImportInstancedFoliage)
            meta.WorldFlags |= EWorldFlags.InstancedFoliage;
        
        await Exporter.Export(exports, meta);

        if (SupaBase.IsLoggedIn)
        {
            await SupaBase.PostExports(
                exports
                    .Select(export => export.Key.GetPathName())
            );
        }
    }

    private bool IsValidFilePath(string path)
    {
        var isValidExtension = path.EndsWith(".uasset") || path.EndsWith(".umap") || path.EndsWith(".ufont");
        var isOptionalSegment = path.Contains(".o.");
        var isVerse = path.Contains("/_Verse/");
        return isValidExtension && !isOptionalSegment && !isVerse;
    }
    
    private Func<FlatItem, bool> CreateAssetFilter((string, bool, ObservableCollection<string>) items)
    {
        var (filter, useRegex, gameNames) = items;
        if (string.IsNullOrWhiteSpace(filter)) return asset => gameNames.Count > 0 && MiscExtensions.FilterAny(asset.Path, gameNames);
        
        if (useRegex)
        {
            return asset => Regex.IsMatch(asset.Path, filter) && MiscExtensions.FilterAny(asset.Path, gameNames);
        }

        return asset => MiscExtensions.Filter(asset.Path, filter) && MiscExtensions.FilterAny(asset.Path, gameNames);
    }
    
    // scuffed fix to get filter to update
    public void FakeRefreshFilters()
    {
        var temp = SelectedGameNames;
        SelectedGameNames = [];
        SelectedGameNames = temp;
    }
}