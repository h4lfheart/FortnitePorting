using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using FortnitePorting.Framework;
using FortnitePorting.Models.Files;
using CUE4Parse.UE4.VirtualFileSystem;

namespace FortnitePorting.Services;

public partial class FilesService : ObservableObject, IService, IResettable
{
    private const int ProgressBatchSize = 2048;

    public FileNode RootFileNode { get; } = new("Files", string.Empty, ENodeType.Folder);

    public SourceCache<FlatItem, string> FlatViewAssetCache = new(item => item.Path);

    [ObservableProperty, NotifyPropertyChangedFor(nameof(LoadingPercentageText))] private int _loadedFiles;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(LoadingPercentageText))] private int _totalFiles = int.MaxValue;
    [ObservableProperty] private bool _isLoading = true;

    public string LoadingPercentageText => $"{(LoadedFiles == 0 && TotalFiles == 0 ? 0 : LoadedFiles * 100f / TotalFiles):N0}%";

    public void Reset()
    {
        RootFileNode.Clear();
        FlatViewAssetCache.Dispose();
        FlatViewAssetCache = new SourceCache<FlatItem, string>(item => item.Path);
        LoadedFiles = 0;
        TotalFiles = int.MaxValue;
        IsLoading = true;
    }

    public async Task Initialize()
    {
        if (UEParse.Provider is null) return;

        await TaskService.RunDispatcherAsync(() =>
        {
            IsLoading = true;
            LoadedFiles = 0;
        });

        await TaskService.RunAsync(BuildFileList);

        await TaskService.RunDispatcherAsync(() => IsLoading = false);
    }

    private void BuildFileList()
    {
        var totalFiles = UEParse.Provider.Files.Count;
        TaskService.RunDispatcher(() => TotalFiles = totalFiles);

        var flatItems = new List<FlatItem>(totalFiles);
        var processed = 0;
        var lastReported = 0;

        foreach (var (_, file) in UEParse.Provider.Files)
        {
            processed++;

            var path = file.Path;
            if (IsValidFilePath(path))
            {
                var sourceVfsName = string.Empty;
                if (file is VfsEntry vfsEntry)
                    sourceVfsName = vfsEntry.Vfs.Name;

                flatItems.Add(new FlatItem(path, sourceVfsName));

                var parts = path.Split("/", StringSplitOptions.RemoveEmptyEntries);

                var parentNode = RootFileNode;
                for (var i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    if (!parentNode.TryGetChild(part, out var childNode))
                    {
                        var isFile = i == parts.Length - 1;
                        var nodePath = isFile
                            ? path
                            : string.Concat(path.AsSpan(0, path.IndexOf(part, StringComparison.Ordinal)), part);
                        childNode = new FileNode(part, nodePath, isFile ? ENodeType.File : ENodeType.Folder, sourceVfsName);
                        parentNode.AddChild(part, childNode);
                    }

                    parentNode = childNode;
                }
            }

            if (processed - lastReported >= ProgressBatchSize || processed == totalFiles)
            {
                lastReported = processed;
                var report = processed;
                TaskService.RunDispatcher(() => LoadedFiles = report);
            }
        }

        FlatViewAssetCache.Edit(updater => updater.AddOrUpdate(flatItems));
    }

    private bool IsValidFilePath(string path)
    {
        var isValidExtension = path.EndsWith(".uasset") || path.EndsWith(".umap") || path.EndsWith(".ufont");
        var isVerse = path.Contains("/_Verse/");
        return isValidExtension && !isVerse;
    }
}
