using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using FortnitePorting;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using Material.Icons;
using ReactiveUI;

namespace FortnitePorting.Models.Files;

public partial class FileBrowserContext : ObservableObject
{
    private static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromMilliseconds(250);

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

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasFlatSearchFilter), nameof(ShowFlatPathPlain), nameof(FlatViewEmptyMessage))]
    private string _flatSearchFilter = string.Empty;
    [ObservableProperty] private string _fileSearchFilter = string.Empty;
    
    [ObservableProperty] private bool _isDragDropEnabled = false;

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

    public bool HasFlatSearchFilter => !string.IsNullOrWhiteSpace(FlatSearchFilter);
    public bool ShowFlatPathPlain => !HasFlatSearchFilter;
    public string FlatViewEmptyMessage => HasFlatSearchFilter || SelectedVfsCount > 0
        ? "No Files Found"
        : "Search or select a container to find files";

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
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasVisibleVfsGroups))]
    private ObservableCollection<VfsFilterGroup> _vfsFilterGroups = [];
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ContainersButtonText), nameof(FlatViewEmptyMessage))]
    private int _selectedVfsCount;
    [ObservableProperty] private string _vfsFilterSearchText = string.Empty;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(VfsSortIcon))] private bool _descendingVfsSort;

    public MaterialIconKind VfsSortIcon =>
        DescendingVfsSort ? MaterialIconKind.SortDescending : MaterialIconKind.SortAscending;

    public string ContainersButtonText => SelectedVfsCount > 0
        ? $"Containers ({SelectedVfsCount})"
        : "Containers";

    public bool HasVisibleVfsGroups => VfsFilterGroups.Count > 0;

    private readonly Dictionary<string, VfsFilterGroup> _vfsGroupsByName = new();
    
    public bool HasSelectedFiles => UseFlatView
        ? SelectedFlatViewItems.Count > 0
        : SelectedFileViewItems.Any(x => x.Type == ENodeType.File);

    public bool CanGoBack => BackStackCount > 0;
    public bool CanGoForward => ForwardStackCount > 0;
    public bool CanGoToParent => CurrentFolder.Parent is not null;

    private readonly Stack<TreeItem> _backStack = new();
    private readonly Stack<TreeItem> _forwardStack = new();
    private readonly HashSet<FlatItem> _selectedFlatViewSet = [];
    private HashSet<string> _activeVfsNames = [];

    private readonly TreeItem _parentTreeItem = new("Files", ENodeType.Folder)
    {
        Expanded = true,
        Selected = true
    };

    private IDisposable? _subscriptions;
    private CancellationTokenSource? _vfsSelectionCts;

    public void Reset()
    {
        _vfsSelectionCts?.Cancel();
        _vfsSelectionCts?.Dispose();
        _vfsSelectionCts = null;

        _subscriptions?.Dispose();
        _subscriptions = null;

        _parentTreeItem.DetachSourceNodes();
        _parentTreeItem.FolderChildren.Clear();
        _parentTreeItem.FileChildCount = 0;
        _parentTreeItem.FolderChildCount = 0;

        CurrentFolder = _parentTreeItem;

        TreeViewCollection = [];
        FileViewCollection = [];
        FileViewStack = [];
        SelectedFlatViewItems = [];
        SelectedFileViewItems = [];
        VfsFilterCollection = [];
        VfsFilterGroups = [];
        _vfsGroupsByName.Clear();
        SelectedVfsCount = 0;
        VfsFilterSearchText = string.Empty;
        DescendingVfsSort = false;
        FlatViewCollection = new ReadOnlyObservableCollection<FlatItem>([]);
        _selectedFlatViewSet.Clear();
        _activeVfsNames = [];

        _backStack.Clear();
        _forwardStack.Clear();
    }

    public void Initialize(string? startPath = null)
    {
        VfsFilterCollection = [..UEParse.Provider.MountedVfs
            .Where(vfs => vfs.FileCount > 0)
            .Select(vfs => new VfsFilterItem(vfs.Name))
            .Where(vfs => !vfs.VfsName.Contains("plugin.utoc", StringComparison.OrdinalIgnoreCase))
            .OrderBy(vfs => vfs.VfsName)
        ];

        RefreshVisibleVfsFilters();
        RefreshActiveVfsNames();

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

        var flatViewSubscription = AppServices.Files.FlatViewAssetCache.Connect()
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Filter(assetFilter)
            .Sort(SortExpressionComparer<FlatItem>.Ascending(x => x.Path))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out var flatCollection)
            .Subscribe();

        FlatViewCollection = flatCollection;

        var vfsSelectionSubscription = vfsCheckedChanges
            .Subscribe(_ => ScheduleVfsSelectionChanged());

        _subscriptions = new CompositeDisposable(flatViewSubscription, vfsSelectionSubscription);

        RealizeTreeChildren(_parentTreeItem);

        _parentTreeItem.Expanded = true;
        _parentTreeItem.Selected = true;

        TreeViewCollection = [_parentTreeItem];
        
        if (!string.IsNullOrEmpty(startPath))
            JumpTo(startPath);
        else
            LoadFileItems(_parentTreeItem, addToStackHistory: false);
        
        CurrentFolder = _parentTreeItem;

        SelectedFileViewItems.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasSelectedFiles));
        AttachFlatSelectionTracking(SelectedFlatViewItems);
    }

    public bool IsFlatItemSelected(FlatItem item) => _selectedFlatViewSet.Contains(item);

    partial void OnSelectedFlatViewItemsChanged(ObservableCollection<FlatItem> value)
    {
        AttachFlatSelectionTracking(value);
        OnPropertyChanged(nameof(HasSelectedFiles));
    }

    private void AttachFlatSelectionTracking(ObservableCollection<FlatItem> collection)
    {
        collection.CollectionChanged -= OnFlatSelectionCollectionChanged;
        collection.CollectionChanged += OnFlatSelectionCollectionChanged;
        SyncSelectedFlatViewSet(collection);
    }

    private void OnFlatSelectionCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (sender is ObservableCollection<FlatItem> collection)
            SyncSelectedFlatViewSet(collection);
        OnPropertyChanged(nameof(HasSelectedFiles));
    }

    private void SyncSelectedFlatViewSet(ObservableCollection<FlatItem> collection)
    {
        _selectedFlatViewSet.Clear();
        foreach (var item in collection)
            _selectedFlatViewSet.Add(item);
    }
    
    public string[] GetSelectedFilePaths() => UseFlatView
        ? SelectedFlatViewItems
            .Select(x => x.Path)
            .ToArray()
        : SelectedFileViewItems
            .Where(x => x.Type == ENodeType.File)
            .Select(x => x.FilePath)
            .ToArray();

    public void JumpTo(string path) => FileViewJumpTo(path);

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
            .Where(IsVfsVisible)
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
            .Where(IsVfsVisible)
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

    public async Task RealizeFileDataAsync(TreeItem item)
    {
        if (item.FileBitmap is not null) return;
        if (item.Type == ENodeType.Folder) return;

        var (icon, _, exportType) = await UEParse.ResolveGameFileAsync(item.FilePath);
        item.FileBitmap = icon;
        item.ExportType = exportType ?? item.Extension;
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

    [RelayCommand]
    public void ClearVfsSelection()
    {
        foreach (var item in VfsFilterCollection)
            item.IsChecked = false;
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
            if (!IsVfsVisible(child)) continue;

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

        var hasFilter = !string.IsNullOrWhiteSpace(filter);
        if (!hasFilter && activeVfs.Count == 0)
            return _ => false;

        return asset =>
        {
            var vfsMatch = activeVfs.Count == 0
                           || activeVfs.Contains(asset.VfsName);
            if (!vfsMatch)
                return false;

            if (!hasFilter)
                return true;

            try
            {
                return useRegex
                    ? Regex.IsMatch(asset.Path, filter, RegexOptions.None, RegexMatchTimeout)
                    : MiscExtensions.Filter(asset.Path, filter);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        };
    }

    partial void OnFileSearchFilterChanged(string value) => FilterFiles();
    partial void OnFileTypeFilterChanged(EFileFilterType value) => FilterFiles();
    partial void OnVfsFilterSearchTextChanged(string value) => RefreshVisibleVfsFilters();
    partial void OnDescendingVfsSortChanged(bool value) => RefreshVisibleVfsFilters();

    partial void OnUseFlatViewChanged(bool value)
    {
        if (!value)
            ScheduleVfsSelectionChanged();
    }

    private void ScheduleVfsSelectionChanged()
    {
        _vfsSelectionCts?.Cancel();
        _vfsSelectionCts?.Dispose();
        _vfsSelectionCts = new CancellationTokenSource();
        var token = _vfsSelectionCts.Token;

        _ = DebounceVfsSelectionAsync(token);
    }

    private async Task DebounceVfsSelectionAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(50, token);
            await TaskService.RunDispatcherAsync(OnVfsSelectionChanged, DispatcherPriority.Background);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void OnVfsSelectionChanged()
    {
        RefreshActiveVfsNames();
        SelectedVfsCount = _activeVfsNames.Count;
        RefreshGroupSelectedCounts();
        ApplyFileViewVfsFilter();
    }

    private void RefreshGroupSelectedCounts()
    {
        foreach (var group in _vfsGroupsByName.Values)
            group.SelectedCount = VfsFilterCollection.Count(item => item.Group == group.Title && item.IsChecked);
    }

    private void RefreshActiveVfsNames()
    {
        _activeVfsNames = VfsFilterCollection
            .Where(x => x.IsChecked)
            .Select(x => x.VfsName)
            .ToHashSet();
    }

    private void RefreshVisibleVfsFilters()
    {
        IEnumerable<VfsFilterItem> items = VfsFilterCollection;

        if (!string.IsNullOrWhiteSpace(VfsFilterSearchText))
        {
            items = items.Where(item =>
                MiscExtensions.Filter(item.VfsName, VfsFilterSearchText)
                || MiscExtensions.Filter(item.Group, VfsFilterSearchText));
        }

        var comparer = new CustomComparer<string>(ComparisonExtensions.CompareNatural);
        var groups = new ObservableCollection<VfsFilterGroup>();

        foreach (var grouping in items.GroupBy(item => item.Group).OrderBy(group => group.Key, comparer))
        {
            if (!_vfsGroupsByName.TryGetValue(grouping.Key, out var group))
            {
                group = new VfsFilterGroup(grouping.Key);
                _vfsGroupsByName[grouping.Key] = group;
            }

            var orderedItems = DescendingVfsSort
                ? grouping.OrderByDescending(item => item.VfsName, comparer)
                : grouping.OrderBy(item => item.VfsName, comparer);

            group.Items = new ObservableCollection<VfsFilterItem>(orderedItems);
            group.SelectedCount = VfsFilterCollection.Count(item => item.Group == group.Title && item.IsChecked);
            groups.Add(group);
        }

        VfsFilterGroups = groups;
    }

    private bool IsVfsVisible(FileNode node)
    {
        if (_activeVfsNames.Count == 0) return true;
        return node.VfsNames.Overlaps(_activeVfsNames);
    }

    private void ApplyFileViewVfsFilter()
    {
        if (UseFlatView) return;
        if (TreeViewCollection.Count == 0) return;

        RebuildFolderChildren(_parentTreeItem);

        var folder = CurrentFolder;
        while (folder != _parentTreeItem
               && folder.SourceNode is not null
               && !IsVfsVisible(folder.SourceNode))
        {
            folder = folder.Parent ?? _parentTreeItem;
        }

        if (SelectedFileViewItems.Count > 0)
            SelectedFileViewItems.Clear();

        if (folder != CurrentFolder)
            LoadFileItems(folder, addToStackHistory: false);
        else
            FilterFiles();
    }

    private void RebuildFolderChildren(TreeItem treeItem)
    {
        var sourceNode = treeItem == _parentTreeItem ? AppServices.Files.RootFileNode : treeItem.SourceNode;
        if (sourceNode is null) return;

        var existingByName = treeItem.FolderChildren.ToDictionary(x => x.Name);
        var expandedChildren = treeItem.FolderChildren
            .Where(x => x.Expanded)
            .Select(x => x.Name)
            .ToHashSet();

        treeItem.FolderChildren.Clear();
        treeItem.FolderChildCount = sourceNode.FolderChildCount;
        treeItem.FileChildCount = sourceNode.FileChildCount;

        var folderNodes = sourceNode.Children.Values
            .Where(n => n.Type == ENodeType.Folder)
            .Where(IsVfsVisible)
            .OrderBy(n => n.Name, new CustomComparer<string>(ComparisonExtensions.CompareNatural));

        foreach (var node in folderNodes)
        {
            if (!existingByName.TryGetValue(node.Name, out var child))
            {
                child = new TreeItem(
                    node.Name,
                    ENodeType.Folder,
                    node.Path,
                    treeItem,
                    sourceNode: node,
                    onExpand: RealizeTreeChildren);
            }

            treeItem.AddFolderChild(child);

            if (expandedChildren.Contains(node.Name) || child.Expanded)
                RebuildFolderChildren(child);
        }
    }

    private void FilterFiles()
    {
        if (UseFlatView) return;

        var sourceNode = CurrentFolder == _parentTreeItem ? AppServices.Files.RootFileNode : CurrentFolder.SourceNode;
        if (sourceNode is null) return;

        var items = sourceNode.Children.Values
            .Where(IsVfsVisible)
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
                try
                {
                    return UseRegex
                        ? Regex.IsMatch(item.FilePath, FileSearchFilter, RegexOptions.None, RegexMatchTimeout)
                        : MiscExtensions.Filter(item.FilePath, FileSearchFilter);
                }
                catch (RegexMatchTimeoutException)
                {
                    return false;
                }
                catch (ArgumentException)
                {
                    return false;
                }
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
