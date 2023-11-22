using System.Collections.Generic;
using System.IO;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;

namespace FortnitePorting.Extensions;

public class HybridFileProvider : AbstractVfsFileProvider
{
    private readonly DirectoryInfo WorkingDirectory;
    private const bool CaseInsensitive = true;
    private static readonly SearchOption SearchOption = SearchOption.AllDirectories;

    // Live
    public HybridFileProvider(VersionContainer? version = null) : base(CaseInsensitive, version)
    {
    }

    // Local + Custom
    public HybridFileProvider(string directory, VersionContainer? version = null) : base(CaseInsensitive, version)
    {
        WorkingDirectory = new DirectoryInfo(directory);
    }

    public override void Initialize()
    {
        if (!WorkingDirectory.Exists) throw new DirectoryNotFoundException($"Provided installation folder does not exist: {WorkingDirectory.FullName}");

        var files = new Dictionary<string, GameFile>();
        foreach (var file in WorkingDirectory.EnumerateFiles("*.*", SearchOption))
        {
            var extension = file.Extension.SubstringAfter('.').ToLower();
            if (extension is not ("pak" or "utoc")) continue;
            if (file.Name.Contains(".o.")) continue; // no optional segments pls !!

            RegisterVfs(file.FullName, new Stream[] { file.OpenRead() }, it => new FStreamArchive(it, File.OpenRead(it), Versions));
        }

        _files.AddFiles(files);
    }
}