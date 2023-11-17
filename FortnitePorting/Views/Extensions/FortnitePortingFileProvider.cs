using System.Collections.Generic;
using System.IO;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using MercuryCommons.Framework.Unreal;

namespace FortnitePorting.Views.Extensions;

public class FortnitePortingFileProvider : AbstractVfsFileProvider
{
    private readonly DirectoryInfo WorkingDirectory;
    private const bool CaseInsensitive = true;
    private static readonly SearchOption SearchOption = SearchOption.AllDirectories;

    // Live
    public FortnitePortingFileProvider(VersionContainer? version = null) : base(CaseInsensitive, version)
    {
        
    }

    // Local + Custom
    public FortnitePortingFileProvider(string directory, VersionContainer? version = null) : base(CaseInsensitive, version)
    {
        WorkingDirectory = new DirectoryInfo(directory);
    }

    public override void Initialize()
    {
        if (!WorkingDirectory.Exists)
        {
            throw new DirectoryNotFoundException($"Provided installation folder does not exist: {WorkingDirectory.FullName}");
        }
        
        var files = new Dictionary<string, GameFile>();
        foreach (var file in WorkingDirectory.EnumerateFiles("*.*", SearchOption))
        {
            var extension = file.Extension.SubstringAfter('.').ToLower();
            if (extension is not ("pak" or "utoc")) continue;
            
            RegisterVfs(file.FullName, new Stream[] { file.OpenRead() }, it => new FStreamArchive(it, File.OpenRead(it), Versions));
        }
        
        _files.AddFiles(files);
    }
}