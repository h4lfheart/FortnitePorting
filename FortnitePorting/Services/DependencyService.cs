using System;
using System.IO;
using Avalonia.Platform;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Services;
using Ionic.Zip;

namespace FortnitePorting.Services;

public static class DependencyService
{
    public static readonly FileInfo BinkaFile = new(Path.Combine(DataFolder.FullName, "binkadec.exe"));
    public static readonly FileInfo VGMStreamFile = new(Path.Combine(DataFolder.FullName, "vgmstream-cli.exe"));

    public static void EnsureDependencies()
    {
        TaskService.Run(() =>
        {
            EnsureResourceBased("Assets/Dependencies/binkadec.exe", BinkaFile);
            EnsureVGMStream();
        });
    }

    private static void EnsureResourceBased(string path, FileInfo targetFile)
    {
        var assetStream = AssetLoader.Open(new Uri($"avares://FortnitePorting/{path}"));
        if (targetFile is { Exists: true, Length: > 0 } && targetFile.GetHash() == assetStream.GetHash()) return;

        targetFile.Delete();
        File.WriteAllBytes(targetFile.FullName, assetStream.ReadToEnd());
    }

    private static void EnsureVGMStream()
    {
        if (VGMStreamFile is { Exists: true, Length: > 0 } ) return;
        
        var file = ApiVM.DownloadFile("https://github.com/vgmstream/vgmstream/releases/latest/download/vgmstream-win.zip", DataFolder);
        if (!file.Exists || file.Length == 0) return;
        
        var zip = ZipFile.Read(file.FullName);
        foreach (var zipFile in zip)
        {
            zipFile.Extract(DataFolder.FullName, ExtractExistingFileAction.OverwriteSilently);
        }
    }
}