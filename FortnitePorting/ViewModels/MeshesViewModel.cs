using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Controls.Avalonia;
using FortnitePorting.Framework;
using FortnitePorting.Services;

namespace FortnitePorting.ViewModels;

public partial class MeshesViewModel : ViewModelBase
{
    [ObservableProperty] private SuppressibleObservableCollection<TreeNode> treeItems = new();
    [ObservableProperty] private SuppressibleObservableCollection<FlatViewItem> assetItems = new();
    
    public override async Task Initialize()
    {
        HomeVM.Update("Loading Mesh Entries");

        var nodes = new SuppressibleObservableCollection<TreeNode>();
        nodes.SetSuppression(true);
        
        var assets = new SuppressibleObservableCollection<FlatViewItem>();
        assets.SetSuppression(true);

        await TaskService.RunDispatcherAsync(() =>
        {
            void InvokeOnCollectionChanged(TreeNode item)
            {
                item.Children.SetSuppression(false);
                if (item.Children.Count == 0) return;

                item.Children.InvokeOnCollectionChanged();
                foreach (var folderItem in item.Children)
                {
                    InvokeOnCollectionChanged(folderItem);
                }
            }

            var eggos = 0;
            foreach (var (_, file) in CUE4ParseVM.Provider.Files)
            {
                
                if (eggos > 5000) break;
                var path = file.Path;
                if (!(path.EndsWith(".uasset") || path.EndsWith(".umap")) || path.EndsWith(".o.uasset")) continue;
                
                assets.AddSuppressed(new FlatViewItem(path));

                TreeNode? foundNode;
                var folders = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var builder = new StringBuilder();
                var children = nodes;
                
                for (var i = 0; i < folders.Length; i++)
                {
                    var folder = folders[i];
                    builder.Append(folder).Append('/');
                    foundNode = children.FirstOrDefault(x => x.Name == folder);

                    if (foundNode is null)
                    {
                        var nodePath = builder.ToString();
                        if (i == folders.Length - 1) // actual asset, last in folder arr
                        {
                            foundNode = new TreeNode(folder.Replace(".uasset", string.Empty), nodePath[..^1], ENodeType.File);
                        }
                        else
                        {
                            foundNode = new TreeNode(folder, nodePath[..^1], ENodeType.Folder);
                        }

                        foundNode.Children.SetSuppression(true);
                        children.AddSuppressed(foundNode);
                    }

                    children = foundNode.Children;
                }

                eggos++;
            }
            
            AssetItems.AddRange(assets);
            TreeItems.AddRange(nodes);

            foreach (var child in TreeItems)
            {
                InvokeOnCollectionChanged(child);
            }
        });
        
        
        HomeVM.Update(string.Empty);
    }
}

public partial class TreeNode : ObservableObject
{
    public SuppressibleObservableCollection<TreeNode> Children { get; set; } = new();

    [ObservableProperty] private ENodeType nodeType;
    [ObservableProperty] private string name;
    [ObservableProperty] private string localPath;
    [ObservableProperty] private string fullPath;
    
    public bool IsFolder => NodeType == ENodeType.Folder;

    public TreeNode(string name, string localPath, ENodeType nodeType)
    {
        Name = name;
        LocalPath = localPath;
        NodeType = nodeType;

        if (NodeType is ENodeType.File)
        {
            FullPath = LocalPath.Replace(".uasset", string.Empty);
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