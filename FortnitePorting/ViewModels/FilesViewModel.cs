using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Engine;
using DynamicData;
using DynamicData.Binding;
using FortnitePorting.Application;
using FortnitePorting.Controls.Avalonia;
using FortnitePorting.Export;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Framework.Controls;
using FortnitePorting.Framework.Extensions;
using FortnitePorting.Services;
using FortnitePorting.Framework.Services;
using ReactiveUI;
using Serilog;
using TexturePreviewWindow = FortnitePorting.Windows.TexturePreviewWindow;

namespace FortnitePorting.ViewModels;

public partial class FilesViewModel : ViewModelBase
{
    [ObservableProperty] private EExportTargetType exportType = EExportTargetType.Blender;
    [ObservableProperty] private TreeNodeItem? selectedTreeItem;
    [ObservableProperty] private FlatViewItem? selectedFlatViewItem;
    [ObservableProperty] private ObservableCollection<FlatViewItem> selectedExportItems = [];

    [ObservableProperty] private SuppressibleObservableCollection<TreeNodeItem> treeItems = [];
    [ObservableProperty] private ReadOnlyObservableCollection<FlatViewItem> assetItemsTarget;
    [ObservableProperty] private SourceList<FlatViewItem> assetItemsSource = new();
    [ObservableProperty] private SuppressibleObservableCollection<FlatViewItem> assetItems = [];
    [ObservableProperty] private string searchFilter = string.Empty;

    [ObservableProperty] private float scanPercentage;
    [ObservableProperty] private int exportChunks;
    [ObservableProperty] private int exportProgress;
    [ObservableProperty] private bool isExporting;

    public bool Started;
    private HashSet<string> AssetsToLoad;
    private HashSet<string> AssetsToRemove;
    private IObservable<Func<FlatViewItem, bool>> AssetFilter;

    private readonly HashSet<string> Filters = new()
    {
       // Folders
        "Engine/",
        "/Playsets",
        "/DataTables",
        "/TextureData",
        "/ActorBlueprints",
        "/Physics",
        "/_Verse",
        "/VectorFields",

        // Prefixes
        "/PID_",
        "/PPID_",
        "/MI_",
        "/MF_",
        "/NS_",
        "/P_",
        "/TD_",
        "/MPC_",
        "/BP_",

        // Suffixes
        "_Physics",
        "_AnimBP",
        "_PhysMat",
        "_PoseAsset",
        "_CapeColliders",
        "_PhysicalMaterial",
        "_BuiltData",

        // Other
        "PlaysetGrenade",
        "NaniteDisplacement"
    };

    private readonly Type[] ValidExportTypes =
    {
        typeof(USkeletalMesh),
        typeof(UStaticMesh),
        typeof(UWorld),
        typeof(UTexture),
        typeof(UAnimationAsset),
        typeof(USoundCue),
        typeof(USoundWave),
    };

    public override async Task Initialize()
    {
        HomeVM.Update("Loading Files");

        AssetsToLoad = new HashSet<string>();
        AssetsToRemove = CUE4ParseVM.AssetRegistry.Select(x => CUE4ParseVM.Provider.FixPath(x.ObjectPath) + ".uasset").ToHashSet();

        foreach (var (filePath, file) in CUE4ParseVM.Provider.Files)
            if (IsValidPath(filePath))
                AssetsToLoad.Add(file.Path);

        HomeVM.Update(string.Empty);
    }

    public async Task LoadFiles()
    {
        if (Started) return;
        
        Started = true;
        AssetFilter = this
            .WhenAnyValue(x => x.SearchFilter)
            .Throttle(TimeSpan.FromMilliseconds(250))
            .Select(CreateAssetFilter);

        AssetItemsSource.Connect()
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Filter(AssetFilter)
            .Sort(SortExpressionComparer<FlatViewItem>.Ascending(x => x.Path))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out var tempTarget)
            .DisposeMany()
            .Subscribe();
        AssetItemsTarget = tempTarget;

        TreeItems.SetSuppression(true);
        AssetItems.SetSuppression(true);

        AssetsVM.CurrentLoader?.Pause.Pause();
        await TaskService.RunDispatcherAsync(() =>
        {
            foreach (var path in AssetsToLoad)
            {
                AssetItems.AddSuppressed(new FlatViewItem(path));

                var folderNames = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var builder = new StringBuilder();
                var children = TreeItems; // start at root
                for (var i = 0; i < folderNames.Length; i++)
                {
                    var folder = folderNames[i];
                    builder.Append(folder).Append('/');
                    var foundNode = children.FirstOrDefault(x => x.Name == folder);

                    if (foundNode is null)
                    {
                        var nodePath = builder.ToString();
                        foundNode = new TreeNodeItem(nodePath[..^1], i == folderNames.Length - 1 ? ENodeType.File : ENodeType.Folder);
                        foundNode.Children.SetSuppression(true);
                        children.AddSuppressed(foundNode);
                    }

                    children = foundNode.Children;
                }
               
            }
        });
        
        TreeItems.InvokeOnCollectionChanged();
        foreach (var child in TreeItems) child.InvokeOnCollectionChanged();
        
        AssetItems.InvokeOnCollectionChanged();
        AssetItemsSource.AddRange(AssetItems);
        
        AssetsVM.CurrentLoader?.Pause.Unpause();
    }

    [RelayCommand]
    public async Task Export()
    {
        var exports = new List<KeyValuePair<UObject, EAssetType>>();
        foreach (var item in SelectedExportItems)
        {
            var asset = await CUE4ParseVM.Provider.LoadObjectAsync(FixPath(item.Path));
            if (asset is UVirtualTextureBuilder vtBuilder) asset = vtBuilder.Texture;
            
            var assetType = asset switch
            {
                USkeletalMesh => EAssetType.Mesh,
                UStaticMesh => EAssetType.Mesh,
                UTexture => EAssetType.Texture,
                UWorld => EAssetType.World,
                UAnimationAsset => EAssetType.Animation,
                USoundWave => EAssetType.Sound,
                USoundCue => EAssetType.Sound,
                _ => EAssetType.None
            };

            if (assetType is EAssetType.None)
            {
                MessageWindow.Show("Invalid Export", $"Exporting {asset.Name} of type {asset.ExportType} is not supported.");
            }
            else
            {
                exports.Add(new KeyValuePair<UObject, EAssetType>(asset, assetType));
            }
        }
        
        ExportChunks = 1;
        ExportProgress = 0;
        IsExporting = true;
        await ExportService.ExportAsync(exports, ExportType);
        IsExporting = false;
    }

    [RelayCommand]
    public async Task ScanContent()
    {
        await TaskService.RunAsync(async () =>
        {
            var total = CUE4ParseVM.Provider.Files.Count;
            var index = 0.0f;
            foreach (var (filePath, file) in CUE4ParseVM.Provider.Files)
            {
                index++;

                try
                {
                    if (!IsValidPath(filePath)) continue;

                    var fixPath = FixPath(filePath);
                    var percentage = index / total * 100;
                    if (Math.Abs(ScanPercentage - percentage) > 0.01f) ScanPercentage = percentage;

                    var obj = await CUE4ParseVM.Provider.TryLoadObjectAsync(file.PathWithoutExtension);
                    if (obj is null || !ValidExportTypes.Any(type => obj.GetType().IsAssignableTo(type))) AppSettings.Current.HiddenFilePaths.Add(fixPath);
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }
            }

            ScanPercentage = 100.0f;
            AppVM.RestartWithMessage("File Scanning Completed", "The file scanning process has finished. FortnitePorting will now restart.");
        });
    }

    public async Task Preview()
    {
        var item = SelectedExportItems.FirstOrDefault();
        var asset = await CUE4ParseVM.Provider.LoadObjectAsync(FixPath(item.Path));
        if (asset is UVirtualTextureBuilder vtBuilder) asset = vtBuilder.Texture;

        switch (asset)
        {
            case UTexture texture:
            {
                await TaskService.RunDispatcherAsync(() =>
                {
                    new TexturePreviewWindow(texture).Show();
                });
                break;
            }

            default:
            {
                MessageWindow.Show("Not Implemented Yet", $"A file previewer for {asset.ExportType} {asset.Name} has not been implemented or is not supported.");
                break;
            }
        }
        
    }

    public void TreeViewJumpTo(string directory)
    {
        var children = TreeItems; // start at root

        var i = 0;
        var folders = directory.Split('/');
        while (true)
        {
            foreach (var folder in children)
            {
                if (!folder.Name.Equals(folders[i], StringComparison.OrdinalIgnoreCase))
                    continue;

                if (folder.NodeType == ENodeType.File)
                {
                    SelectedTreeItem = folder;
                    return;
                }

                folder.Expanded = true;
                children = folder.Children;
                break;
            }

            i++;
            if (children.Count == 0) break;
        }
    }

    public void FlatViewJumpTo(string directory)
    {
        var children = AssetItemsTarget;
        foreach (var child in children)
        {
            if (!child.Path.Equals(directory)) continue;

            SelectedFlatViewItem = child;
            break;
        }
    }

    private bool IsValidPath(string path)
    {
        var isValidPathType = (path.EndsWith(".uasset") || path.EndsWith(".umap")) && !path.Contains(".o.");
        var isInRegistry = AssetsToRemove.Contains(path);
        var isFiltered = Filters.Any(filter => path.Contains(filter, StringComparison.OrdinalIgnoreCase));
        var isFilteredByScan = AppSettings.Current.HiddenFilePaths.Contains(path);
        return isValidPathType && !isInRegistry && !isFiltered && !isFilteredByScan;
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

    private Func<FlatViewItem, bool> CreateAssetFilter(string filter)
    {
        return asset => MiscExtensions.Filter(asset.Path, filter);
    }
}

public partial class TreeNodeItem : ObservableObject
{
    public SuppressibleObservableCollection<TreeNodeItem> Children { get; set; } = new();

    [ObservableProperty] private ENodeType nodeType;
    [ObservableProperty] private string name;
    [ObservableProperty] private FlatViewItem pathInfo;
    [ObservableProperty] private bool selected;
    [ObservableProperty] private bool expanded;

    public bool IsFolder => NodeType == ENodeType.Folder;
    public bool IsFile => NodeType == ENodeType.File;

    public TreeNodeItem(string path, ENodeType nodeType)
    {
        PathInfo = new FlatViewItem(path);
        Name = path.SubstringAfterLast("/");
        NodeType = nodeType;
    }

    public void InvokeOnCollectionChanged()
    {
        Children.SetSuppression(false);
        if (Children.Count == 0) return;

        Children.Sort(x => x.Name);
        Children.SortDescending(x => x.IsFolder);
        Children.InvokeOnCollectionChanged();
        foreach (var folder in Children) folder.InvokeOnCollectionChanged();
    }
}

public partial class FlatViewItem : ObservableObject
{
    [ObservableProperty] private string pathWithoutExtension;
    [ObservableProperty] private string extension;
    [ObservableProperty] private string path;

    public FlatViewItem(string path)
    {
        Path = path;

        PathWithoutExtension = path.SubstringBeforeLast(".");
        Extension = path.SubstringAfterLast(".");
    }
}

public enum ENodeType
{
    Folder,
    File
}