using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.AppUtils;
using FortnitePorting.Views.Controls;

namespace FortnitePorting.ViewModels;

public class MeshAssetViewModel : ObservableObject
{
    public bool HasStarted;
    public ListCollectionView View;

    public async Task Initialize()
    {
        var loadTime = new Stopwatch();
        loadTime.Start();
        HasStarted = true;

        var treeItems = new SuppressibleObservableCollection<TreeItem>();
        treeItems.SetSuppression(true);

        var assetItems = new SuppressibleObservableCollection<AssetItem>();
        assetItems.SetSuppression(true);
        
        Application.Current.Dispatcher.Invoke(() =>
        {
            static void InvokeOnCollectionChanged(TreeItem item)
            {
                item.Children.SetSuppression(false);
                if (item.Children.Count == 0) return;

                item.Children.InvokeOnCollectionChanged();
                foreach (var folderItem in item.Children)
                {
                    InvokeOnCollectionChanged(folderItem);
                }
            }

            View = new ListCollectionView(AppVM.MainVM.Meshes) { SortDescriptions = { new SortDescription("IsFolder", ListSortDirection.Descending), new SortDescription("Header", ListSortDirection.Ascending) } };

            foreach (var entry in AppVM.CUE4ParseVM.MeshEntries)
            {
                assetItems.AddSuppressed(new AssetItem(entry));

                TreeItem? foundNode;
                var folders = entry.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var builder = new StringBuilder();
                var children = treeItems;

                for (var i = 0; i < folders.Length; i++)
                {
                    var folder = folders[i];
                    builder.Append(folder).Append('/');
                    foundNode = children.FirstOrDefault(x => x.Header == folder);

                    if (foundNode is null)
                    {
                        var nodePath = builder.ToString();
                        if (i == folders.Length - 1) // actual asset, last in folder arr
                        {
                            foundNode = new TreeItem(folder.Replace(".uasset", string.Empty), nodePath[..^1], ETreeItemType.Asset);
                        }
                        else
                        {
                            foundNode = new TreeItem(folder, nodePath[..^1], ETreeItemType.Folder);
                        }

                        foundNode.Children.SetSuppression(true);
                        children.AddSuppressed(foundNode);
                    }

                    children = foundNode.Children;
                }
            }

            AppVM.MainVM.Assets.AddRange(assetItems);
            AppVM.MainVM.Meshes.AddRange(treeItems);

            foreach (var child in AppVM.MainVM.Meshes)
            {
                InvokeOnCollectionChanged(child);
            }
        });
        
        loadTime.Stop();
        AppLog.Information($"Loaded {AppVM.CUE4ParseVM.MeshEntries.Count} Meshes in {Math.Round(loadTime.Elapsed.TotalSeconds, 3)}s");
        
    }
}

public partial class TreeItem : ObservableObject
{
    public ListCollectionView View { get; }
    public SuppressibleObservableCollection<TreeItem> Children { get; }
    public ETreeItemType AssetType { get; }
    public string LocalPath { get; }
    public string? FullPath; // Asset Only
    public bool IsFolder => AssetType == ETreeItemType.Folder;

    [ObservableProperty] private string header;
    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private bool isExpanded;

    public TreeItem(string header, string localPath, ETreeItemType assetType)
    {
        Header = header;
        LocalPath = localPath;
        AssetType = assetType;
        Children = new SuppressibleObservableCollection<TreeItem>();
        View = new ListCollectionView(Children)
        {
            SortDescriptions =
            {
                new SortDescription("IsFolder", ListSortDirection.Descending),
                new SortDescription("Header", ListSortDirection.Ascending)
            }
        };

        if (AssetType == ETreeItemType.Asset)
        {
            FullPath = LocalPath.Replace(".uasset", string.Empty);
        }
    }
}

public partial class AssetItem : ObservableObject
{
    [ObservableProperty] private string pathWithoutExtension;
    [ObservableProperty] private string path;

    public AssetItem(string path)
    {
        Path = path;
        PathWithoutExtension = path.Replace(".uasset", string.Empty);
    }
}