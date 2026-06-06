using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.UObject;
using DynamicData;
using DynamicData.Binding;
using FortnitePorting.Application;
using FortnitePorting.Exporting;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Files;
using FortnitePorting.Shared.Extensions;
using Material.Icons;
using ReactiveUI;

namespace FortnitePorting.Models.Files;

public partial class FileBrowserContext : ObservableObject
{
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
    private readonly Dictionary<EFileFilterType, string[]> _searchTermsByFilter = new()
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SearchText), nameof(FlatViewToggleIcon), nameof(FlatViewToggleToolTip), nameof(HasSelectedFiles))]
    private bool _useFlatView = false;

    public MaterialIconKind FlatViewToggleIcon =>
        UseFlatView ? MaterialIconKind.Folder : MaterialIconKind.FormatListBulleted;
    public string FlatViewToggleToolTip => UseFlatView ? "File View" : "Flat View";

    [ObservableProperty] private bool _useRegex = false;

    [ObservableProperty] private ObservableCollection<FlatItem> _selectedFlatViewItems = [];
    [ObservableProperty] private ReadOnlyObservableCollection<FlatItem> _flatViewCollection = new([]);

    [ObservableProperty] private ObservableCollection<TreeItem> _selectedFileViewItems = [];
    [ObservableProperty] private ObservableCollection<TreeItem> _fileViewCollection = [];
    [ObservableProperty] private ObservableCollection<TreeItem> _fileViewStack = [];
    [ObservableProperty] private ObservableCollection<TreeItem> _treeViewCollection = [];

    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanGoToParent))] private TreeItem _currentFolder;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanGoBack))] private int _backStackCount = 0;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanGoForward))] private int _forwardStackCount = 0;
    
    [ObservableProperty] private ObservableCollection<VfsFilterItem> _vfsFilterCollection = [];
    
    public bool HasSelectedFiles => UseFlatView
        ? SelectedFlatViewItems.Count > 0
        : SelectedFileViewItems.Any(x => x.Type == ENodeType.File);

    public bool CanGoBack => BackStackCount > 0;
    public bool CanGoForward => ForwardStackCount > 0;
    public bool CanGoToParent => CurrentFolder.Parent is not null;

    private readonly Stack<TreeItem> _backStack = new();
    private readonly Stack<TreeItem> _forwardStack = new();

    private readonly TreeItem _parentTreeItem = new("Files", ENodeType.Folder)
    {
        Expanded = true,
        Selected = true
    };

    public void Initialize(string? startPath = null)
    {
        VfsFilterCollection = [..UEParse.Provider.MountedVfs
            .Where(vfs => vfs.FileCount > 0)
            .Select(vfs => new VfsFilterItem(vfs.Name))
            .Where(vfs => !vfs.VfsName.Contains("plugin.utoc", StringComparison.OrdinalIgnoreCase))
            .OrderBy(vfs => vfs.Title)
        ];
       
        var vfsCheckedChanges = VfsFilterCollection
            .Select(item => item
                .WhenAnyValue(x => x.IsChecked)
                .Select(_ => Unit.Default))
            .Merge();

        var assetFilter = this
            .WhenAnyValue(
                ctx => ctx.FlatSearchFilter,
                ctx => ctx.UseRegex)
            .CombineLatest(vfsCheckedChanges.StartWith(Unit.Default))
            .Select(_ => CreateAssetFilter(FlatSearchFilter, UseRegex, VfsFilterCollection));

        AppServices.Files.FlatViewAssetCache.Connect()
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Filter(assetFilter)
            .Sort(SortExpressionComparer<FlatItem>.Ascending(x => x.Path))
            .Bind(out var flatCollection)
            .Subscribe();

        FlatViewCollection = flatCollection;

        RealizeTreeChildren(_parentTreeItem);

        _parentTreeItem.Expanded = true;
        _parentTreeItem.Selected = true;

        TreeViewCollection = [_parentTreeItem];
        
        if (!string.IsNullOrEmpty(startPath))
            JumpTo(startPath);
        else
            LoadFileItems(_parentTreeItem, addToStackHistory: false);
        
        CurrentFolder = _parentTreeItem;

        SelectedFileViewItems.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(HasSelectedFiles));
        SelectedFlatViewItems.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(HasSelectedFiles));
    }
    
    public string[] GetSelectedFilePaths() => UseFlatView
        ? SelectedFlatViewItems
            .Select(x => x.Path)
            .ToArray()
        : SelectedFileViewItems
            .Where(x => x.Type == ENodeType.File)
            .Select(x => x.FilePath)
            .ToArray();

    public void JumpTo(string path)
    {
        if (UseFlatView)
            FlatViewJumpTo(path);
        else
            FileViewJumpTo(path);
    }

    public void ClearSearchFilter()
    {
        SearchText = string.Empty;
        if (UseFlatView)
            FlatSearchFilter = string.Empty;
        else
            FileSearchFilter = string.Empty;
    }

    public void RealizeTreeChildren(TreeItem treeItem)
    {
        var sourceNode = treeItem == _parentTreeItem ? AppServices.Files.RootFileNode : treeItem.SourceNode;
        if (sourceNode is null) return;

        treeItem.FolderChildCount = sourceNode.FolderChildCount;
        treeItem.FileChildCount = sourceNode.FileChildCount;

        var folderNodes = sourceNode.Children.Values
            .Where(n => n.Type == ENodeType.Folder)
            .OrderBy(n => n.Name, new CustomComparer<string>(ComparisonExtensions.CompareNatural));

        foreach (var node in folderNodes)
        {
            if (treeItem.TryGetFolderChild(node.Name, out _)) continue;

            var child = new TreeItem(
                node.Name,
                ENodeType.Folder,
                node.Path,
                treeItem,
                sourceNode: node,
                onExpand: RealizeTreeChildren);

            treeItem.AddFolderChild(child);
        }
    }

    public void LoadFileItems(TreeItem item, bool addToStackHistory = true)
    {
        if (addToStackHistory && CurrentFolder != item)
        {
            _backStack.Push(CurrentFolder);
            BackStackCount = _backStack.Count;
            _forwardStack.Clear();
            ForwardStackCount = 0;
        }

        CurrentFolder = item;

        var sourceNode = item == _parentTreeItem ? AppServices.Files.RootFileNode : item.SourceNode;
        if (sourceNode is null)
        {
            FileViewCollection = [];
            FileViewStack = [];
            return;
        }

        var children = sourceNode.Children.Values
            .OrderByDescending(n => n.Type == ENodeType.Folder)
            .ThenBy(n => n.Name, new CustomComparer<string>(ComparisonExtensions.CompareNatural))
            .Select(n =>
            {
                if (n.Type == ENodeType.Folder && item.TryGetFolderChild(n.Name, out var existing))
                    return existing;
                return new TreeItem(n.Name, n.Type, n.Path, item, sourceNode: n, onExpand: RealizeTreeChildren);
            });

        FileViewCollection = new ObservableCollection<TreeItem>(children);

        var stack = new List<TreeItem>();
        var parent = item;
        while (parent != null)
        {
            stack.Insert(0, parent);
            parent = parent.Parent;
        }

        FileViewStack = new ObservableCollection<TreeItem>(stack);
        FileSearchText = string.Empty;
        FileTypeFilter = EFileFilterType.All;
    }

    public void RealizeFileData(TreeItem item)
    {
        if (item.FileBitmap is not null) return;
        if (item.Type == ENodeType.Folder) return;

        if (UEParse.Provider.TryLoadPackage(item.FilePath, out var package))
        {
            for (var i = 0; i < package.ExportMapLength; i++)
            {
                var pointer = new FPackageIndex(package, i + 1).ResolvedObject;
                if (pointer?.Object is null) continue;
                if (!pointer.Name.Text.Equals(item.NameWithoutExtension) &&
                    !pointer.Name.Text.Equals(item.NameWithoutExtension + "_C")) continue;

                var obj = ((AbstractUePackage) package).ConstructObject(pointer.Class, package);
                item.ExportType = obj.ExportType;

                if (obj is UTexture2D && pointer.TryLoad(out var textureObj) &&
                    textureObj is UTexture2D texture &&
                    texture.Decode(maxMipSize: 128) is { } decodedTexture)
                {
                    item.FileBitmap = decodedTexture.ToWriteableBitmap();
                    break;
                }

                var assetLoader = AssetLoading.Categories
                    .SelectMany(category => category.Loaders)
                    .FirstOrDefault(loader => loader.ClassNames.Contains(obj.ExportType));
                if (assetLoader is not null && pointer.TryLoad(out var assetObj))
                {
                    item.FileBitmap = assetLoader.IconHandler(assetObj)?.Decode(maxMipSize: 128)?.ToWriteableBitmap();
                    break;
                }

                if (obj.GetEditorIconBitmap() is { } objectBitmap)
                {
                    item.FileBitmap = objectBitmap;
                    break;
                }

                if (Exporter.DetermineExportType(obj) is var exportType and not EExportType.None
                    && $"avares://FortnitePorting/Assets/FN/{exportType}.png" is { } exportIconPath
                    && AssetLoader.Exists(new Uri(exportIconPath)))
                {
                    item.FileBitmap = ImageExtensions.AvaresBitmap(exportIconPath);
                    break;
                }
            }

            if (item.ExportType is null &&
                new FPackageIndex(package, 1).ResolvedObject is { } zeroPointer)
            {
                var zeroObj = ((AbstractUePackage) package).ConstructObject(zeroPointer.Class, package);
                item.ExportType = zeroObj.ExportType;
            }
        }

        item.FileBitmap ??= ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/Unreal/DataAsset_64x.png");
        item.ExportType ??= item.Extension;
    }

    public void FileViewJumpTo(string path)
    {
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var fileName = parts.Last();
        var folderPath = string.Join('/', parts.SkipLast(1));

        var folderItem = TreeViewJumpTo(folderPath);
        var parentFolder = folderItem ?? _parentTreeItem;

        DeselectTree(_parentTreeItem);

        var ancestor = parentFolder;
        while (ancestor is not null)
        {
            ancestor.Expanded = true;
            ancestor.Selected = ancestor == parentFolder;
            ancestor = ancestor.Parent;
        }

        LoadFileItems(parentFolder);
        UseFlatView = false;

        var fileItem = FileViewCollection.FirstOrDefault(x => x.Name == fileName);
        if (fileItem is not null)
            SelectedFileViewItems = [fileItem];
    }

    public void FlatViewJumpTo(string directory)
    {
        var foundItem = AppServices.Files.FlatViewAssetCache.Lookup(directory);
        if (!foundItem.HasValue) return;

        FlatSearchFilter = string.Empty;
        FlatSearchText = string.Empty;
        SelectedFlatViewItems = [foundItem.Value];
        UseFlatView = true;
    }

    public TreeItem? TreeViewJumpTo(string directory)
    {
        var parts = directory.Split('/', StringSplitOptions.RemoveEmptyEntries);

        var current = _parentTreeItem;
        foreach (var part in parts)
        {
            RealizeTreeChildren(current);
            if (!current.TryGetFolderChild(part, out var foundChild))
                return null;
            if (foundChild.Type == ENodeType.Folder)
                foundChild.Expanded = true;
            current = foundChild;
        }

        return current;
    }
    
    [RelayCommand]
    public void GoBack()
    {
        if (_backStack.Count == 0) return;
        var prev = _backStack.Pop();
        BackStackCount = _backStack.Count;
        _forwardStack.Push(CurrentFolder);
        ForwardStackCount = _forwardStack.Count;
        LoadFileItems(prev, addToStackHistory: false);
        SelectedFileViewItems.Clear();
    }

    [RelayCommand]
    public void GoForward()
    {
        if (_forwardStack.Count == 0) return;
        var next = _forwardStack.Pop();
        ForwardStackCount = _forwardStack.Count;
        _backStack.Push(CurrentFolder);
        BackStackCount = _backStack.Count;
        LoadFileItems(next, addToStackHistory: false);
        SelectedFileViewItems.Clear();
    }

    [RelayCommand]
    public void GoToParent()
    {
        if (CurrentFolder.Parent is null) return;
        LoadFileItems(CurrentFolder.Parent);
        SelectedFileViewItems.Clear();
    }

    private void DeselectTree(TreeItem item)
    {
        item.Selected = false;
        foreach (var child in item.FolderChildren)
            DeselectTree(child);
    }

    public IEnumerable<TreeItem> GetAllFileDescendants(FileNode node, TreeItem parent)
    {
        foreach (var child in node.Children.Values)
        {
            if (child.Type == ENodeType.File)
            {
                yield return new TreeItem(child.Name, ENodeType.File, child.Path, parent,
                    sourceNode: child, onExpand: RealizeTreeChildren);
            }
            else
            {
                parent.TryGetFolderChild(child.Name, out var folderItem);
                folderItem ??= new TreeItem(child.Name, ENodeType.Folder, child.Path, parent,
                    sourceNode: child, onExpand: RealizeTreeChildren);

                foreach (var descendant in GetAllFileDescendants(child, folderItem))
                    yield return descendant;
            }
        }
    }
    
    private Func<FlatItem, bool> CreateAssetFilter(string filter, bool useRegex, IEnumerable<VfsFilterItem> vfsItems)
    {
        var activeVfs = vfsItems
            .Where(x => x.IsChecked)
            .Select(x => x.VfsName)
            .ToHashSet();

        return asset =>
        {
            var pathMatch = useRegex
                ? Regex.IsMatch(asset.Path, filter)
                : MiscExtensions.Filter(asset.Path, filter);

            var vfsMatch = activeVfs.Count == 0
                           || activeVfs.Contains(asset.VfsName);

            return pathMatch && vfsMatch;
        };
    }

    partial void OnFileSearchFilterChanged(string value) => FilterFiles();
    partial void OnFileTypeFilterChanged(EFileFilterType value) => FilterFiles();

    private void FilterFiles()
    {
        if (UseFlatView) return;

        var sourceNode = CurrentFolder == _parentTreeItem ? AppServices.Files.RootFileNode : CurrentFolder.SourceNode;
        if (sourceNode is null) return;

        var items = sourceNode.Children.Values
            .Select(n =>
            {
                if (n.Type == ENodeType.Folder && CurrentFolder.TryGetFolderChild(n.Name, out var existing))
                    return existing;
                return new TreeItem(n.Name, n.Type, n.Path, CurrentFolder,
                    sourceNode: n, onExpand: RealizeTreeChildren);
            })
            .Where(item =>
            {
                if (string.IsNullOrWhiteSpace(FileSearchFilter)) return true;
                return UseRegex
                    ? Regex.IsMatch(item.FilePath, FileSearchFilter)
                    : MiscExtensions.Filter(item.FilePath, FileSearchFilter);
            })
            .Where(item =>
            {
                if (FileTypeFilter is EFileFilterType.All) return true;
                if (item.Type is ENodeType.Folder) return false;
                return _searchTermsByFilter[FileTypeFilter].Any(filter =>
                    (item.ExportType?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
                    || item.Extension.Contains(filter, StringComparison.OrdinalIgnoreCase));
            })
            .OrderByDescending(item => item.Type == ENodeType.Folder)
            .ThenBy(item => item.Name, new CustomComparer<string>(ComparisonExtensions.CompareNatural));

        FileViewCollection = new ObservableCollection<TreeItem>(items);
    }
}