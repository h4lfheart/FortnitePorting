using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using DynamicData;
using DynamicData.Binding;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Exporting;
using FortnitePorting.Exporting.Context;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.Files;
using FortnitePorting.Models.Information;
using FortnitePorting.Models.Unreal;
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
    [ObservableProperty] private EExportLocation _assetExportLocation = EExportLocation.Blender;
    [ObservableProperty] private EExportLocation _dataExportLocation = EExportLocation.AssetsFolder;
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsAssetExportTarget))] private EExportTarget _exportTarget = EExportTarget.Asset;
    
    public bool IsAssetExportTarget => ExportTarget == EExportTarget.Asset;

    public EnumRecord[] FolderExportLocations =>
        Enum.GetValues<EExportLocation>()
            .Where(val => val.IsFolder)
            .Select(val => val.ToEnumRecord())
            .ToArray();
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(SearchText))] private string _flatSearchText = string.Empty;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(SearchText))] private string _fileSearchText = string.Empty;

    public string SearchText
    {
        get => UseFlatView ? FlatSearchText : FileSearchText;
        set
        {
            if (UseFlatView)
                FlatSearchText = value;
            else
                FileSearchText = value;
        }
    }

    [ObservableProperty] private string _flatSearchFilter = string.Empty;
    [ObservableProperty] private string _fileSearchFilter = string.Empty;
    
    [ObservableProperty] private EFileFilterType _fileTypeFilter = EFileFilterType.All;
    private Dictionary<EFileFilterType, string[]> _searchTermsByFilter = new()
    {
        [EFileFilterType.All] = [],
        [EFileFilterType.Texture] = ["Texture"],
        [EFileFilterType.Mesh] = ["StaticMesh", "SkeletalMesh"],
        [EFileFilterType.Skeleton] = ["Skeleton"],
        [EFileFilterType.Animation] = ["AnimSequence", "AnimMontage"],
        [EFileFilterType.Material] = ["Material"],
        [EFileFilterType.Sound] = ["SoundWave", "SoundCue"],
        [EFileFilterType.Font] = ["Font", "ufont"],
        [EFileFilterType.PoseAsset] = ["PoseAsset"],
        [EFileFilterType.Map] = ["World", "umap"]
    };

    [ObservableProperty, NotifyPropertyChangedFor(nameof(SearchText))] private bool _useFlatView = false;
    [ObservableProperty] private bool _useRegex = false;
    [ObservableProperty] private bool _isLoading = true;
    
    [ObservableProperty] private List<FlatItem> _selectedFlatViewItems = [];
    [ObservableProperty] private ReadOnlyObservableCollection<FlatItem> _flatViewCollection = new([]);
    
    [ObservableProperty] private List<TreeItem> _selectedFileViewItems = [];
    [ObservableProperty] private ObservableCollection<TreeItem> _fileViewCollection = [];
    [ObservableProperty] private ObservableCollection<TreeItem> _fileViewStack = [];
    [ObservableProperty] private ObservableCollection<TreeItem> _treeViewCollection = new([]);
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(LoadingPercentageText))] private int _loadedFiles;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(LoadingPercentageText))] private int _totalFiles = int.MaxValue;
    public string LoadingPercentageText => $"{(LoadedFiles == 0 && TotalFiles == 0 ? 0 : LoadedFiles * 100f / TotalFiles):N0}%";

    private readonly TreeItem _parentTreeItem = new("Files", ENodeType.Folder)
    {
        Expanded = true,
        Selected = true
    };
    
    private TreeItem _currentFolder;

    private readonly SourceCache<FlatItem, string> FlatViewAssetCache = new(item => item.Path);
    
    public override async Task Initialize()
    {
        BuildFileList();
        
        var assetFilter = this
            .WhenAnyValue(viewModel => viewModel.FlatSearchFilter, viewmodel => viewmodel.UseRegex)
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
            
        IsLoading = false;
    }

    private void BuildFileList()
    {
        TotalFiles = UEParse.Provider.Files.Count;
        foreach (var (_, file) in UEParse.Provider.Files)
        {
            LoadedFiles++;
            
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
        SearchText = string.Empty;
        if (UseFlatView)
            FlatSearchFilter = string.Empty;
        else
            FileSearchFilter = string.Empty;
    }
    
    public void LoadFileItems(TreeItem item)
    {
        _currentFolder = item;

        var allChildren = item.GetAllChildren();
        
        FileViewCollection = new ObservableCollection<TreeItem>(allChildren);
        
        var newStack = new List<TreeItem>();
        var parent = item;
        while (parent != null)
        {
            newStack.Insert(0, parent);
            parent = parent.Parent;
        }
        
        FileViewStack = new ObservableCollection<TreeItem>(newStack);
        FileSearchText = string.Empty;
        FileTypeFilter = EFileFilterType.All;
    }

    public void RealizeFileData(ref TreeItem item)
    {
        if (item.Type == ENodeType.Folder)
        {
            item.GetAllChildren()
                .Where(item => item.Type is ENodeType.File)
                .ForEach(item => RealizeFileData(ref item));    
            return;
        }
        
        if (item.FileBitmap is not null) return;
                    
        if (UEParse.Provider.TryLoadPackage(item.FilePath, out var package))
        {
            for (var i = 0; i < package.ExportMapLength; i++)
            {
                var pointer = new FPackageIndex(package, i + 1).ResolvedObject;
                if (pointer?.Object is null) continue;
                if (!pointer.Name.Text.Equals(item.NameWithoutExtension) && !pointer.Name.Text.Equals(item.NameWithoutExtension + "_C")) continue;
                        
                // use texture as preview
                var obj = ((AbstractUePackage) package).ConstructObject(pointer.Class?.Object?.Value as UStruct, package);
                item.ExportType = obj.ExportType;
                
                if (obj is UTexture2D && pointer.TryLoad(out var textureObj) && textureObj is UTexture2D texture && texture.Decode(maxMipSize: 128) is { } decodedTexture)
                {
                    item.FileBitmap = decodedTexture.ToWriteableBitmap();
                    break;
                }
                
                // use asset loader icon getter as preview
                var assetLoader = AssetLoading.Categories
                    .SelectMany(category => category.Loaders)
                    .FirstOrDefault(loader => loader.ClassNames.Contains(obj.ExportType));
                if (assetLoader is not null && pointer.TryLoad(out var assetObj))
                {
                    item.FileBitmap = assetLoader.IconHandler(assetObj)?.Decode(maxMipSize: 128)?.ToWriteableBitmap();
                    break;
                }
                    
                // use engine-mapped export type as prevoiew
                if (obj.GetEditorIconBitmap() is { } objectBitmap)
                {
                    item.FileBitmap = objectBitmap;
                    break;
                }

                // use fortnite-mapped export type as preview (is this needed with asset loader preview as well?)
                if (Exporter.DetermineExportType(obj) is var exportType and not EExportType.None 
                    && $"avares://FortnitePorting/Assets/FN/{exportType.ToString()}.png" is { } exportIconPath 
                    && AssetLoader.Exists(new Uri(exportIconPath)))
                {
                    item.FileBitmap = ImageExtensions.AvaresBitmap(exportIconPath);
                    break;
                }
            }

            if (item.ExportType is null && new FPackageIndex(package, 1).ResolvedObject is { } zeroPointer)
            {
                var zeroObj = ((AbstractUePackage) package).ConstructObject(zeroPointer.Class?.Object?.Value as UStruct, package);
                item.ExportType = zeroObj.ExportType;
            }
        }

        item.FileBitmap ??= ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/Unreal/DataAsset_64x.png");
        item.ExportType ??= item.Extension;
    }

    public void FileViewJumpTo(string path)
    { 
        var treeItem = TreeViewJumpTo(path);
        if (treeItem?.Parent is null) return;
        
        LoadFileItems(treeItem.Parent);
        UseFlatView = false;

        SelectedFileViewItems = [treeItem];
    }

    public void FlatViewJumpTo(string directory)
    {
        var foundItem = FlatViewAssetCache.Lookup(directory);
        if (!foundItem.HasValue) return;

        SelectedFlatViewItems = [foundItem.Value];
        UseFlatView = true;
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

    partial void OnFileSearchFilterChanged(string value)
    {
        FilterFiles();
    }

    partial void OnFileTypeFilterChanged(EFileFilterType value)
    {
        FilterFiles();
    }

    private void FilterFiles()
    {
        if (UseFlatView) return;
        
        RealizeFileData(ref _currentFolder);

        var items = _currentFolder.GetAllChildren()
            .Where(item =>
            {
                if (string.IsNullOrWhiteSpace(FileSearchFilter))
                    return true;
                
                return UseRegex
                    ? Regex.IsMatch(item.FilePath, FileSearchFilter)
                    : MiscExtensions.Filter(item.FilePath, FileSearchFilter);
            })
            .Where(item =>
            {
                if (FileTypeFilter is EFileFilterType.All)
                    return true;
                
                if (item.Type is ENodeType.Folder) 
                    return false;
                
                if (item.ExportType is null)
                    Log.Information(item.Name);
                return _searchTermsByFilter[FileTypeFilter].Any(filter => (item.ExportType?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) 
                                                                          || item.Extension.Contains(filter, StringComparison.OrdinalIgnoreCase));
            })
            .OrderByDescending(item => item.Type == ENodeType.Folder)
            .ThenBy(item => item.Name, new CustomComparer<string>(ComparisonExtensions.CompareNatural));
        
        FileViewCollection = new ObservableCollection<TreeItem>(items);
    }

    [RelayCommand]
    public async Task OpenSettings()
    {
        Navigation.App.Open<ExportSettingsView>();
        Navigation.ExportSettings.Open(AssetExportLocation);
    }
    
    [RelayCommand]
    public async Task SetAssetExportLocation(EExportLocation location)
    {
        AssetExportLocation = location;
    }
    
    [RelayCommand]
    public async Task SetDataExportLocation(EExportLocation location)
    {
        DataExportLocation = location;
    }

    [RelayCommand]
    public async Task Properties()
    {
        var selectedItemPath = UseFlatView ? SelectedFlatViewItems.FirstOrDefault()?.Path : SelectedFileViewItems.FirstOrDefault(file => file.Type == ENodeType.File)?.FilePath;
        if (selectedItemPath is null) return;

        try
        {
            if (UEParse.Provider.TryLoadObjectExports(selectedItemPath, out var exports))
            {
                var json = JsonConvert.SerializeObject(exports, Formatting.Indented);
                await TaskService.RunDispatcherAsync(() => PropertiesPreviewWindow.Preview(selectedItemPath.SubstringAfterLast("/").SubstringBefore("."), json));
            }
        }
        catch (Exception)
        {
            Info.Message("Properties", $"Failed to preview {selectedItemPath}");
        }
    }
    
    [RelayCommand]
    public async Task Preview()
    {
        var selectedPaths = UseFlatView 
            ? SelectedFlatViewItems.Select(file => file.Path) 
            : SelectedFileViewItems.Where(file => file.Type == ENodeType.File).Select(file => file.FilePath);

        foreach (var path in selectedPaths)
        {
            var basePath = Exporter.FixPath(path);
            
            UObject? asset;
            if (path.EndsWith(".umap"))
            {
                var package = await UEParse.Provider.LoadPackageAsync(basePath);
                asset = package.GetExports().OfType<UWorld>().FirstOrDefault();
            }
            else
            {
                asset = await UEParse.Provider.SafeLoadPackageObjectAsync(basePath);
                asset ??= await UEParse.Provider.SafeLoadPackageObjectAsync($"{basePath}.{basePath.SubstringAfterLast("/")}_C");
            }

            if (asset is null)
            {
                await Properties();
                return;
            }

            await PreviewAsset(asset);
        }
        
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
                Info.Dialog($"Preview {instance.Name}", "What asset type would you like to preview?", buttons: 
                [
                    new DialogButton
                    {
                        Text = "Material Properties",
                        Action = () => TaskService.Run(Properties)
                    },
                    new DialogButton
                    {
                        Text = "Material Node Tree",
                        Action = () =>
                        {
                            UUnrealMaterial? parentMaterial = instance;
                            while (parentMaterial is UMaterialInstanceConstant parentMaterialInstance)
                            {
                                parentMaterial = parentMaterialInstance.Parent;
                            }
                        
                            if (parentMaterial is not null)
                                MaterialPreviewWindow.Preview(parentMaterial);
                        }
                    },
                ]);
                
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
                SoundCuePreviewWindow.Preview(soundCue);
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
        switch (ExportTarget)
        {
            case EExportTarget.Asset:
                await ExportAssets();
                break;
            case EExportTarget.Properties:
                await ExportProperties();
                break;
            case EExportTarget.RawData:
                await ExportRawData();
                break;
        }
        
    }

    private async Task ExportAssets()
    {
        var exports = new List<KeyValuePair<UObject, EExportType>>();
        var unsupportedExportTypes = new HashSet<string>();

        var paths = UseFlatView
            ? SelectedFlatViewItems.Select(x => x.Path).ToList()
            : SelectedFileViewItems.Where(x => x.Type == ENodeType.File).Select(x => x.FilePath).ToList();

        var folders = UseFlatView ? [] : SelectedFileViewItems.Where(x => x.Type == ENodeType.Folder);
        foreach (var folder in folders)
        {
            var children = folder.GetAllChildren();
            paths.AddRange(children.Where(x => x.Type == ENodeType.File).Select(x => x.FilePath));
        }
        
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

        var meta = AppSettings.ExportSettings.CreateExportMeta(AssetExportLocation);
        meta.WorldFlags = EWorldFlags.Actors | EWorldFlags.Landscape | EWorldFlags.WorldPartitionGrids | EWorldFlags.HLODs;
        if (meta.Settings.ImportInstancedFoliage)
            meta.WorldFlags |= EWorldFlags.InstancedFoliage;
        
        var exportedProperly = await Exporter.Export(exports, meta);
        if (exportedProperly && SupaBase.IsLoggedIn)
        {
            await SupaBase.PostExports(
                exports
                    .Select(export => export.Key.GetPathName())
            );
        }
    }

    private async Task ExportProperties()
    {
        var paths = UseFlatView
            ? SelectedFlatViewItems.Select(x => x.Path).ToList()
            : SelectedFileViewItems.Where(x => x.Type == ENodeType.File).Select(x => x.FilePath).ToList();
        
        var folders = UseFlatView ? [] : SelectedFileViewItems.Where(x => x.Type == ENodeType.Folder);
        foreach (var folder in folders)
        {
            var children = folder.GetAllChildren();
            paths.AddRange(children.Where(x => x.Type == ENodeType.File).Select(x => x.FilePath));
        }

        if (paths.Count == 0)
        {
            Info.Message("Exporter", "Failed to load any assets for export.", InfoBarSeverity.Warning);
            return;
        }

        var meta = AppSettings.ExportSettings.CreateExportMeta(DataExportLocation);
        if (meta.ExportLocation is EExportLocation.CustomFolder && await App.BrowseFolderDialog() is { } customExportPath)
        {
            meta.CustomPath = customExportPath;
        }
        
        var context = new ExportContext(meta);
        
        foreach (var path in paths)
        {
            var basePath = Exporter.FixPath(path);
            var fileExports = await UEParse.Provider.LoadAllObjectsAsync(basePath);
            
            var exportPath = context.GetExportPath(meta.CustomPath is not null 
                ? basePath.SubstringAfterLast("/").SubstringBeforeLast(".") 
                : basePath , "json");
            
            Log.Information("Exporting Properties: {ExportPath}", exportPath);
            
            var propertiesJson = JsonConvert.SerializeObject(fileExports, Formatting.Indented);
            await File.WriteAllTextAsync(exportPath, propertiesJson);
        }
    }
    
    private async Task ExportRawData()
    {
        var paths = UseFlatView
            ? SelectedFlatViewItems.Select(x => x.Path).ToList()
            : SelectedFileViewItems.Where(x => x.Type == ENodeType.File).Select(x => x.FilePath).ToList();

        var folders = UseFlatView ? [] : SelectedFileViewItems.Where(x => x.Type == ENodeType.Folder);
        foreach (var folder in folders)
        {
            var children = folder.GetAllChildren();
            paths.AddRange(children.Where(x => x.Type == ENodeType.File).Select(x => x.FilePath));
        }
        
        if (paths.Count == 0)
        {
            Info.Message("Exporter", "Failed to load any assets for export.", InfoBarSeverity.Warning);
            return;
        }

        var meta = AppSettings.ExportSettings.CreateExportMeta(DataExportLocation);
        if (meta.ExportLocation is EExportLocation.CustomFolder && await App.BrowseFolderDialog() is { } customExportPath)
        {
            meta.CustomPath = customExportPath;
        }
        
        var context = new ExportContext(meta);
        
        foreach (var path in paths)
        {
            if (!UEParse.Provider.TrySavePackage(path, out var assets))
                continue;

            foreach (var (assetPath, assetData) in assets)
            {
                var exportPath = context.GetExportPath(meta.CustomPath is not null 
                    ? assetPath.SubstringAfterLast("/").SubstringBeforeLast(".") 
                    : assetPath, assetPath.SubstringAfterLast("."));
                
                Log.Information("Exporting Raw Data: {ExportPath}", exportPath);
            
                await File.WriteAllBytesAsync(exportPath, assetData);
            }
        }
    }

    private bool IsValidFilePath(string path)
    {
        var isValidExtension = path.EndsWith(".uasset") || path.EndsWith(".umap") || path.EndsWith(".ufont");
        var isVerse = path.Contains("/_Verse/");
        return isValidExtension && !isVerse;
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