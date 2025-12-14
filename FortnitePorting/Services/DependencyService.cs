using System;
using System.IO;
using System.IO.Compression;
using Avalonia.Platform;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Services;

public class DependencyService : IService
{
    public bool FinishedEnsuring;
    
    public readonly FileInfo BinkaDecoderFile = new(Path.Combine(App.DataFolder.FullName, "binka", "binkadec.exe"));
    public readonly FileInfo RadaDecoderFile = new(Path.Combine(App.DataFolder.FullName, "rada", "radadec.exe"));
    public readonly FileInfo NoodleFile = new(Path.Combine(App.DataFolder.FullName, "noodle.dll"));
    public readonly FileInfo VgmStreamFile = new(Path.Combine(App.DataFolder.FullName, "vgmstream", "vgmstream-cli.exe"));
    
    public readonly DirectoryInfo VgmStreamFolder = new(Path.Combine(App.DataFolder.FullName, "vgmstream"));

    public void Ensure()
    {
        TaskService.Run(() =>
        {
            EnsureResource("Assets/Dependencies/noodle.dll", NoodleFile);
            EnsureResource("Assets/Dependencies/binkadec.exe", BinkaDecoderFile);
            EnsureResource("Assets/Dependencies/radadec.exe", RadaDecoderFile);
            EnsureVgmStream();
            EnsureBlenderExtensions();
            EnsureUnrealPlugins();
            FinishedEnsuring = true;
        });
    }

    private void EnsureResource(string path, FileInfo targetFile)
    {
        var assetStream = AssetLoader.Open(new Uri($"avares://FortnitePorting/{path}"));
        if (targetFile is { Exists: true, Length: > 0 } && targetFile.GetHash() == assetStream.GetHash()) return;

        targetFile.Directory?.Create();
        targetFile.Delete();
        File.WriteAllBytes(targetFile.FullName, assetStream.ReadToEnd());
    }

    private void EnsureVgmStream()
    {
        if (VgmStreamFile is { Exists: true, Length: > 0 } ) return;
        
        VgmStreamFolder.Create();
        var file = Api.DownloadFile("https://github.com/vgmstream/vgmstream/releases/latest/download/vgmstream-win.zip", VgmStreamFolder);
        if (!file.Exists || file.Length == 0) return;
        
        var zip = ZipFile.Open(file.FullName, ZipArchiveMode.Read);
        foreach (var zipFile in zip.Entries)
        {
            using var zipStream = zipFile.Open();
            using var fileStream = new FileStream(Path.Combine(VgmStreamFolder.FullName, zipFile.FullName), FileMode.OpenOrCreate, FileAccess.Write);
            zipStream.CopyTo(fileStream);
        }
    }

    public void EnsureBlenderExtensions()
    {
        var assets = AssetLoader.GetAssets(new Uri("avares://FortnitePorting.Plugins/Blender"), null);
        foreach (var asset in assets)
        {
            var assetStream = AssetLoader.Open(asset);
            var targetFile = new FileInfo(Path.Combine(App.PluginsFolder.FullName, asset.AbsolutePath[1..]));
            if (targetFile is { Exists: true, Length: > 0 } && targetFile.GetHash() == assetStream.GetHash()) continue;
            targetFile.Directory?.Create();
            
            File.WriteAllBytes(targetFile.FullName, assetStream.ReadToEnd());
        }
    }
    
    public void EnsureUnrealPlugins()
    {
        var assets = AssetLoader.GetAssets(new Uri("avares://FortnitePorting.Plugins/Unreal"), null);
        foreach (var asset in assets)
        {
            var assetStream = AssetLoader.Open(asset);
            var targetFile = new FileInfo(Path.Combine(App.PluginsFolder.FullName, asset.AbsolutePath[1..]));
            if (targetFile is { Exists: true, Length: > 0 } && targetFile.GetHash() == assetStream.GetHash()) continue;
            targetFile.Directory?.Create();
            
            File.WriteAllBytes(targetFile.FullName, assetStream.ReadToEnd());
        }
    }
}