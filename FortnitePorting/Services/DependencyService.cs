using System;
using System.IO;
using Avalonia.Platform;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Services;
using Ionic.Zip;

namespace FortnitePorting.Services;

public static class DependencyService
{
    public static bool Finished;
    
    public static readonly FileInfo UpdaterFile = new(Path.Combine(DataFolder.FullName, "release", "FortnitePorting.Updater.exe"));
    
    public static readonly FileInfo BinkaDecoderFile = new(Path.Combine(DataFolder.FullName, "binka", "binkadec.exe"));
    public static readonly FileInfo RadaDecoderFile = new(Path.Combine(DataFolder.FullName, "rada", "radadec.exe"));
    public static readonly FileInfo VgmStreamFile = new(Path.Combine(DataFolder.FullName, "vgmstream-cli.exe"));
    
    public static readonly DirectoryInfo VgmStreamFolder = new(Path.Combine(DataFolder.FullName, "vgmstream"));

    public static void EnsureDependencies()
    {
        TaskService.Run(() =>
        {
            EnsureResourceBased("Assets/Dependencies/binkadec.exe", BinkaDecoderFile);
            EnsureResourceBased("Assets/Dependencies/radadec.exe", RadaDecoderFile);
            EnsureResourceBased("Assets/Dependencies/FortnitePorting.Updater.exe", UpdaterFile);
            EnsureVgmStream();
            EnsureBlenderExtensions();
            EnsureUnrealPlugins();
            Finished = true;
        });
    }

    private static void EnsureResourceBased(string path, FileInfo targetFile)
    {
        var assetStream = AssetLoader.Open(new Uri($"avares://FortnitePorting/{path}"));
        if (targetFile is { Exists: true, Length: > 0 } && targetFile.GetHash() == assetStream.GetHash()) return;

        targetFile.Directory?.Create();
        targetFile.Delete();
        File.WriteAllBytes(targetFile.FullName, assetStream.ReadToEnd());
    }

    private static void EnsureVgmStream()
    {
        if (VgmStreamFile is { Exists: true, Length: > 0 } ) return;
        
        VgmStreamFolder.Create();
        var file = ApiVM.DownloadFile("https://github.com/vgmstream/vgmstream/releases/latest/download/vgmstream-win.zip", VgmStreamFolder);
        if (!file.Exists || file.Length == 0) return;
        
        var zip = ZipFile.Read(file.FullName);
        foreach (var zipFile in zip)
        {
            zipFile.Extract(VgmStreamFolder.FullName, ExtractExistingFileAction.OverwriteSilently);
        }
    }

    public static void EnsureBlenderExtensions()
    {
        var assets = AssetLoader.GetAssets(new Uri("avares://FortnitePorting.Plugins/Blender"), null);
        foreach (var asset in assets)
        {
            var assetStream = AssetLoader.Open(asset);
            var targetFile = new FileInfo(Path.Combine(PluginsFolder.FullName, asset.AbsolutePath[1..]));
            if (targetFile is { Exists: true, Length: > 0 } && targetFile.GetHash() == assetStream.GetHash()) continue;
            targetFile.Directory?.Create();
            
            File.WriteAllBytes(targetFile.FullName, assetStream.ReadToEnd());
        }
    }
    
    public static void EnsureUnrealPlugins()
    {
        var assets = AssetLoader.GetAssets(new Uri("avares://FortnitePorting.Plugins/Unreal"), null);
        foreach (var asset in assets)
        {
            var assetStream = AssetLoader.Open(asset);
            var targetFile = new FileInfo(Path.Combine(PluginsFolder.FullName, asset.AbsolutePath[1..]));
            if (targetFile is { Exists: true, Length: > 0 } && targetFile.GetHash() == assetStream.GetHash()) continue;
            targetFile.Directory?.Create();
            
            File.WriteAllBytes(targetFile.FullName, assetStream.ReadToEnd());
        }
    }
}