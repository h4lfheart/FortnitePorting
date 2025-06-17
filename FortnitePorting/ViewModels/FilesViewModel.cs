using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using DynamicData;
using DynamicData.Binding;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Exporting;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.Files;
using FortnitePorting.Models.Unreal;
using FortnitePorting.Models.Unreal.Material;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Views;
using FortnitePorting.Windows;
using Newtonsoft.Json;
using ReactiveUI;
using Serilog;

namespace FortnitePorting.ViewModels;

public partial class FilesViewModel : ViewModelBase
{
    [ObservableProperty] private EExportLocation _exportLocation = EExportLocation.Blender;
    [ObservableProperty] private string _actualSearchText = string.Empty;

    [ObservableProperty]private string _searchFilter = string.Empty;

    [ObservableProperty] private bool _useFlatView = false;
    [ObservableProperty] private bool _useRegex = false;
    [ObservableProperty] private bool _showLoadingSplash = true;
    
    [ObservableProperty] private List<FlatItem> _selectedFlatViewItems = [];
    [ObservableProperty] private ReadOnlyObservableCollection<FlatItem> _flatViewCollection = new([]);
    
    [ObservableProperty] private List<TreeItem> _selectedFileViewItems = [];
    [ObservableProperty] private ObservableCollection<TreeItem> _fileViewCollection = [];
    [ObservableProperty] private ObservableCollection<TreeItem> _fileViewStack = [];
    [ObservableProperty] private ObservableCollection<TreeItem> _treeViewCollection = new([]);

    private readonly TreeItem _parentTreeItem = new("Files", ENodeType.Folder)
    {
        Expanded = true
    };
    
    private TreeItem _currentFolder;

    private readonly SourceCache<FlatItem, string> FlatViewAssetCache = new(item => item.Path);
    
    public override async Task Initialize()
    {
        BuildTreeStructure();
        
        var assetFilter = this
            .WhenAnyValue(viewModel => viewModel.SearchFilter, viewmodel => viewmodel.UseRegex)
            .Select(CreateAssetFilter);
        
        FlatViewAssetCache.Connect()
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Filter(assetFilter)
            .Sort(SortExpressionComparer<FlatItem>.Ascending(x => x.Path))
            .Bind(out var flatCollection)
            .Subscribe();

        FlatViewCollection = flatCollection;
        TreeViewCollection = [_parentTreeItem];
        LoadFileItems(_parentTreeItem);
            
        ShowLoadingSplash = false;
    }

    private void BuildTreeStructure()
    {
        foreach (var (_, file) in UEParse.Provider.Files)
        {
            var path = file.Path;
            if (!IsValidFilePath(path)) continue;
                
            FlatViewAssetCache.AddOrUpdate(new FlatItem(path));

            var folderNames = path.Split("/", StringSplitOptions.RemoveEmptyEntries);
            
            var parent = _parentTreeItem;
            for (var i = 0; i < folderNames.Length; i++)
            {
                var folderName = folderNames[i];
                if (!parent.TryGetChild(folderName, out var foundNode))
                {
                    var isFile = i == folderNames.Length - 1;
                    var nodePath = isFile ? path : path.SubstringBefore(folderName) + folderName;
                        
                    foundNode = new TreeItem(folderName, isFile ? ENodeType.File : ENodeType.Folder, nodePath, parent);
                    parent.AddChild(folderName, foundNode);
                }

                parent = foundNode;
            }
        }
    }

    public override async Task OnViewOpened()
    {
        Discord.Update($"Browsing {UEParse.Provider.Files.Count} Files");
    }

    public void ClearSearchFilter()
    {
        ActualSearchText = string.Empty;
        SearchFilter = string.Empty;
    }
    
    public void LoadFileItems(TreeItem item)
    {
        _currentFolder = item;

        var allChildren = item.GetAllChildren();

        TaskService.Run(() => LoadFileBitmaps(allChildren));
        
        FileViewCollection = new ObservableCollection<TreeItem>(allChildren);
        
        var newStack = new List<TreeItem>();
        var parent = item;
        while (parent != null)
        {
            newStack.Insert(0, parent);
            parent = parent.Parent;
        }
        
        FileViewStack = new ObservableCollection<TreeItem>(newStack);
    }

    private void LoadFileBitmaps(IEnumerable<TreeItem> fileItems)
    {
        Parallel.ForEach(fileItems, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, childItem => 
            {
                if (childItem.Type == ENodeType.Folder) return;
                if (childItem.FileBitmap is not null) return;
                    
                if (UEParse.Provider.TryLoadPackage(childItem.FilePath, out var package))
                {
                    for (var i = 0; i < package.ExportMapLength; i++)
                    {
                        var pointer = new FPackageIndex(package, i + 1).ResolvedObject;
                        if (pointer?.Object is null) continue;
                        
                        var obj = ((AbstractUePackage) package).ConstructObject(pointer.Class?.Object?.Value as UStruct, package);
                        if (obj.GetEditorIconBitmap() is { } objectBitmap)
                        {
                            childItem.FileBitmap = objectBitmap;
                            break;
                        }
                    }
                }

                childItem.FileBitmap ??= ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/Unreal/DataAsset_64x.png");
            }
        );
    }

    public void FlatViewJumpTo(string directory)
    {
        var foundItem = FlatViewAssetCache.Lookup(directory);
        if (!foundItem.HasValue) return;

        SelectedFlatViewItems = [foundItem.Value];
    }
    
    public TreeItem? TreeViewJumpTo(string directory)
    {
        var folders = directory.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        var current = _parentTreeItem;
        foreach (var folder in folders)
        {
            if (!current.TryGetChild(folder, out var foundChild))
                return null;
                
            if (foundChild.Type == ENodeType.Folder)
                foundChild.Expanded = true;
                
            current = foundChild;
        }

        return current;
    }

    partial void OnSearchFilterChanged(string value)
    {
        if (UseFlatView) return;
        
        if (string.IsNullOrWhiteSpace(SearchFilter))
        {
            LoadFileItems(_currentFolder);
            return;
        }

        var items = FlattenTree(_currentFolder)
            .Where(item =>
                UseRegex ? Regex.IsMatch(item.Name, SearchFilter) : MiscExtensions.Filter(item.Name, SearchFilter))
            .OrderByDescending(item => item.Type == ENodeType.Folder)
            .ThenBy(item => item.Name, new CustomComparer<string>(ComparisonExtensions.CompareNatural));

        TaskService.Run(() => LoadFileBitmaps(items));
        
        FileViewCollection = new ObservableCollection<TreeItem>(items);
        
        IEnumerable<TreeItem> FlattenTree(TreeItem root)
        {
            var stack = new Stack<TreeItem>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                yield return current;

                foreach (var child in current.GetAllChildren().Reverse())
                {
                    stack.Push(child);
                }
            }
        }
    }
    
    [RelayCommand]
    public async Task OpenSettings()
    {
        Navigation.App.Open<ExportSettingsView>();
        Navigation.ExportSettings.Open(ExportLocation);
    }
    
    [RelayCommand]
    public async Task SetExportLocation(EExportLocation location)
    {
        ExportLocation = location;
    }

    [RelayCommand]
    public async Task Properties()
    {
        var selectedItemPath = UseFlatView ? SelectedFlatViewItems.FirstOrDefault()?.Path : SelectedFileViewItems.FirstOrDefault(file => file.Type == ENodeType.File)?.FilePath;
        if (selectedItemPath is null) return;
        
        var assets = await UEParse.Provider.LoadAllObjectsAsync(Exporter.FixPath(selectedItemPath));
        var json = JsonConvert.SerializeObject(assets, Formatting.Indented);
        PropertiesPreviewWindow.Preview(selectedItemPath.SubstringAfterLast("/").SubstringBefore("."), json);
    }
    
    [RelayCommand]
    public async Task Preview()
    {
        var selectedItemPath = UseFlatView ? SelectedFlatViewItems.FirstOrDefault()?.Path : SelectedFileViewItems.FirstOrDefault(file => file.Type == ENodeType.File)?.FilePath;
        if (selectedItemPath is null) return;
        
        var basePath = Exporter.FixPath(selectedItemPath);
            
        UObject? asset = null;
        if (selectedItemPath.EndsWith(".umap"))
        {
            var package = await UEParse.Provider.LoadPackageAsync(basePath);
            asset = package.GetExports().OfType<UWorld>().FirstOrDefault();
        }
        else
        {
            asset = await UEParse.Provider.SafeLoadPackageObjectAsync(basePath);
            asset ??= await UEParse.Provider.SafeLoadPackageObjectAsync($"{basePath}.{basePath.SubstringAfterLast("/")}_C");
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
                if (!UEParse.Provider.MountedVfs.Any(vfs => vfs.Name.Contains(".o.")))
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

        var paths = UseFlatView
            ? SelectedFlatViewItems.Select(x => x.Path)
            : SelectedFileViewItems.Select(x => x.FilePath);
        foreach (var path in paths)
        {
            var basePath = Exporter.FixPath(path);
            
            UObject? asset = null;
            if (path.EndsWith(".umap"))
            {
                asset = await UEParse.Provider.SafeLoadPackageObjectAsync(basePath);
                if (asset is not UWorld)
                {
                    var package = await UEParse.Provider.LoadPackageAsync(basePath);
                    asset = package.GetExports().OfType<UWorld>().FirstOrDefault();
                }
            }
            else
            {
                asset = await UEParse.Provider.SafeLoadPackageObjectAsync(basePath);
                asset ??= await UEParse.Provider.SafeLoadPackageObjectAsync($"{basePath}.{basePath.SubstringAfterLast("/")}_C");
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
            Info.Message("Exporter",
                unsupportedExportTypes.Count == 0
                    ? $"Failed to load any assets for export."
                    : $"Assets with these types do not have exporters: {unsupportedExportTypes.CommaJoin()}.",
                InfoBarSeverity.Warning);

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
    
    private Func<FlatItem, bool> CreateAssetFilter((string, bool) items)
    {
        var (filter, useRegex) = items;
        
        if (useRegex)
        {
            return asset => Regex.IsMatch(asset.Path, filter);
        }

        return asset => MiscExtensions.Filter(asset.Path, filter);
    }
}