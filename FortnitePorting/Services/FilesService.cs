using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.UObject;
using DynamicData;
using FortnitePorting.Exporting;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.Files;
using FortnitePorting.Shared.Extensions;
using Avalonia.Platform;
using CUE4Parse.UE4.VirtualFileSystem;
using Serilog;

namespace FortnitePorting.Services;

public partial class FilesService : ObservableObject, IService
{
    public FileNode RootFileNode { get; } = new("Files", string.Empty, ENodeType.Folder);

    public readonly SourceCache<FlatItem, string> FlatViewAssetCache = new(item => item.Path);

    [ObservableProperty, NotifyPropertyChangedFor(nameof(LoadingPercentageText))] private int _loadedFiles;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(LoadingPercentageText))] private int _totalFiles = int.MaxValue;
    [ObservableProperty] private bool _isLoading = true;

    public string LoadingPercentageText => $"{(LoadedFiles == 0 && TotalFiles == 0 ? 0 : LoadedFiles * 100f / TotalFiles):N0}%";

    public void Initialize()
    {
        if (UEParse.Provider is null) return;

        IsLoading = true;
        BuildFileList();
        IsLoading = false;
    }

    private void BuildFileList()
    {
        TotalFiles = UEParse.Provider.Files.Count;
        foreach (var (_, file) in UEParse.Provider.Files)
        {
            LoadedFiles++;

            var path = file.Path;
            if (!IsValidFilePath(path)) 
                continue;

            
            var sourceVfsName = string.Empty;
            if (file is VfsEntry vfsEntry)
                sourceVfsName = vfsEntry.Vfs.Name;
            
            FlatViewAssetCache.AddOrUpdate(new FlatItem(path, sourceVfsName));

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
    }

    private bool IsValidFilePath(string path)
    {
        var isValidExtension = path.EndsWith(".uasset") || path.EndsWith(".umap") || path.EndsWith(".ufont");
        var isVerse = path.Contains("/_Verse/");
        return isValidExtension && !isVerse;
    }
}