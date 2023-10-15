using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.Utils;
using DynamicData;
using FortnitePorting.Application;
using FortnitePorting.Controls.Assets;
using FortnitePorting.Controls.Avalonia;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using ReactiveUI;
using Serilog;

namespace FortnitePorting.ViewModels;

public partial class MeshesViewModel : ViewModelBase
{
    [ObservableProperty] private EExportType exportType = EExportType.Blender;
    [ObservableProperty] private TreeNodeItem selectedTreeItem;
    [ObservableProperty] private FlatViewItem selectedFlatViewItem;
    [ObservableProperty] private ObservableCollection<FlatViewItem> selectedExportItems = new();
    
    [ObservableProperty] private SuppressibleObservableCollection<TreeNodeItem> treeItems = new();
    [ObservableProperty] private ReadOnlyObservableCollection<FlatViewItem> assetItemsTarget;
    [ObservableProperty] private SourceList<FlatViewItem> assetItemsSource = new();
    [ObservableProperty] private SuppressibleObservableCollection<FlatViewItem> assetItems = new();
    [ObservableProperty] private string searchFilter = string.Empty;
    
    [ObservableProperty] private float scanPercentage = 0.0f;

    public bool Started;
    private HashSet<string> AssetsToLoad;
    private HashSet<string> AssetsToRemove;
    private IObservable<Func<FlatViewItem, bool>> AssetFilter;

    private readonly HashSet<string> Filters = new()
    {
        // Folders
        "Engine/",
        "/Sounds",
        "/Playsets",
        "/UI",
        "/2dAssets",
        "/Textures",
        "/Audio",
        "/Sound",
        "/Materials",
        "/Icons",
        "/Anims",
        "/DataTables",
        "/TextureData",
        "/ActorBlueprints",
        "/Physics",
        "/_Verse",
        "/Animation/Game",
        
        // Prefixes
        "/PID_",
        "/PPID_",
        "/MI_",
        "/MF_",
        "/NS_",
        "/T_",
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

    private string[] MeshTypes =
    {
        "SkeletalMesh",
        "StaticMesh"
    };

    public override async Task Initialize()
    {
        HomeVM.Update("Loading Meshes");
        
        AssetsToLoad = new HashSet<string>();
        AssetsToRemove = CUE4ParseVM.AssetRegistry.Select(x => CUE4ParseVM.Provider.FixPath(x.ObjectPath) + ".uasset").ToHashSet();
        
        foreach (var (filePath, file) in CUE4ParseVM.Provider.Files)
        {
            if (IsValidPath(filePath)) AssetsToLoad.Add(file.Path);
        }

        HomeVM.Update(string.Empty);
    }

    public async Task LoadMeshes()
    {
        if (Started) return;
        Started = true;
        AssetFilter = this.WhenAnyValue(x => x.SearchFilter).Select(CreateAssetFilter);
        
        AssetItemsSource.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Filter(AssetFilter)
            .Bind(out var tempTarget)
            .Subscribe();
        AssetItemsTarget = tempTarget;

        TreeItems.SetSuppression(true);
        AssetItems.SetSuppression(true);
        
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

        foreach (var child in TreeItems)
        {
            child.InvokeOnCollectionChanged();
        }

        TreeItems.InvokeOnCollectionChanged();

        AssetItems.InvokeOnCollectionChanged();
        AssetItemsSource.AddRange(AssetItems);
    }

    [RelayCommand]
    public async Task Export()
    {
        var obj = await CUE4ParseVM.Provider.LoadObjectAsync(SelectedExportItems[0].PathWithoutExtension);
        await ExportService.ExportAsync(obj, EAssetType.Mesh, ExportType);
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
                
                if (!IsValidPath(filePath)) continue;

                var percentage = index / total * 100;
                if (Math.Abs(ScanPercentage - percentage) > 0.01f)
                {
                    ScanPercentage = percentage;
                }
                
                var obj = await CUE4ParseVM.Provider.TryLoadObjectAsync(file.PathWithoutExtension);
                if (obj is null || !MeshTypes.Contains(obj.ExportType)) AppSettings.Current.AssetsThatArentMesh.Add(filePath);
            }

            ScanPercentage = 100.0f;
        });
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
        var isValidPathType = path.EndsWith(".uasset") && !path.EndsWith(".o.uasset");
        var isInRegistry = AssetsToRemove.Contains(path);
        var isFiltered = Filters.Any(filter => path.Contains(filter, StringComparison.OrdinalIgnoreCase));
        var isFilteredByScan = AppSettings.Current.AssetsThatArentMesh.Contains(path);
        return isValidPathType && !isInRegistry && !isFiltered && !isFilteredByScan;
    }

    private static Func<FlatViewItem, bool> CreateAssetFilter(string searchFilter) => asset =>  MiscExtensions.Filter(asset.Path, searchFilter);
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
        foreach (var folder in Children)
        {
            folder.InvokeOnCollectionChanged();
        }
    }
}

public partial class FlatViewItem : ObservableObject
{
    [ObservableProperty] private string pathWithoutExtension;
    [ObservableProperty] private string path;

    public FlatViewItem(string path)
    {
        Path = path;
        PathWithoutExtension = path.Replace(".uasset", string.Empty);
    }
}

public enum ENodeType
{
    Folder,
    File
}