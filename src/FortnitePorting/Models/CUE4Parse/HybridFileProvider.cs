using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using EpicManifestParser.UE;

namespace FortnitePorting.Models.CUE4Parse;

public class HybridFileProvider : AbstractVfsFileProvider
{
    public bool LoadExtraDirectories;
    public bool LoadOnDemandTocs;
    private readonly DirectoryInfo WorkingDirectory;
    private readonly IEnumerable<DirectoryInfo> ExtraDirectories;
    private const SearchOption SearchOption = System.IO.SearchOption.AllDirectories;
    
    private static readonly EnumerationOptions EnumerationOptions = new()
    {
        RecurseSubdirectories = SearchOption == SearchOption.AllDirectories,
        IgnoreInaccessible = true,
    };

    public HybridFileProvider(VersionContainer? version = null)  : base(version, StringComparer.OrdinalIgnoreCase)
    {
        SkipReferencedTextures = true;
    }

    public HybridFileProvider(string directory, List<DirectoryInfo>? extraDirectories = null, VersionContainer? version = null) : this(version)
    {
        WorkingDirectory = new DirectoryInfo(directory);
        ExtraDirectories = extraDirectories?.Where(dir => dir.Exists) ?? [];
        SkipReferencedTextures = true;
    }

    public override void Initialize()
    {
        InitializeAsync().GetAwaiter().GetResult();
    }

    public async Task InitializeAsync()
    {
        if (!WorkingDirectory.Exists) 
            throw new DirectoryNotFoundException($"Provided installation folder does not exist: {WorkingDirectory.FullName}");
        
        await RegisterFiles(WorkingDirectory);
        
        if (LoadExtraDirectories)
        {
            foreach (var extraDirectory in ExtraDirectories)
            {
                await RegisterFiles(extraDirectory);
            }
        }
    }

    public async Task RegisterFiles(DirectoryInfo directory)
    {
        foreach (var file in directory.EnumerateFiles("*.*", EnumerationOptions))
        {
            var extension = file.Extension.SubstringAfter('.').ToLower();
            if (extension is "pak" or "utoc")
            {
                RegisterVfs(file.FullName, [ file.OpenRead() ], it => new FStreamArchive(it, File.OpenRead(it), Versions));
            }

            if (extension is "uondemandtoc" && LoadOnDemandTocs)
            {
                var archive = new FByteArchive(file.FullName, File.ReadAllBytes(file.FullName), Versions);
                var ioChunkToc = new IoChunkToc(archive);
                await RegisterVfsAsync(ioChunkToc);
            }
        }
    }
    
    public async Task RegisterFiles(FBuildPatchAppManifest manifest)
    {
        var targetCacheDirectory = Path.Combine(UEParse.CacheFolder.FullName, "uondemandtoc", manifest.Meta.BuildVersion);
        Directory.CreateDirectory(targetCacheDirectory);
        
        foreach (var file in manifest.Files)
        {
            if (!file.FileName.Contains("FortniteGame/Content/Paks")) continue;
            
            UEParse.UpdateStatus($"Registering On-Demand Archive {file.FileName.SubstringAfterLast("/")}");
            
            var extension = file.FileName.SubstringAfter('.').ToLower();
            if (extension is "pak" or "utoc")
            {
                RegisterVfs(file.FileName, (Stream[]) [file.GetStream()],
                    name => new FStreamArchive(name,
                        manifest.Files.First(subFile => subFile.FileName.Equals(name)).GetStream()));
            }

            if (extension is "uondemandtoc" && LoadOnDemandTocs)
            {
                var targetPath = Path.Combine(targetCacheDirectory, file.FileName.SubstringAfterLast("/"));
                if (!File.Exists(targetPath))
                {
                    await using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write);
                    await file.GetStream().CopyToAsync(fileStream);
                }
                
                var archive = new FByteArchive(targetPath, await File.ReadAllBytesAsync(targetPath), Versions);
                var ioChunkToc = new IoChunkToc(archive);
                await RegisterVfsAsync(ioChunkToc);
            }

        }
    }
}