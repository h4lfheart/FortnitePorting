using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using Ionic.Zip;

namespace FortnitePorting.Services;

public static class DependencyService
{
    public static readonly FileInfo BinkaFile = new(Path.Combine(App.DataFolder.FullName, "binkadec.exe"));

    public static void EnsureDependencies()
    {
        TaskService.Run(EnsureBinka);
    }

    public static async Task EnsureBinka()
    {
        var assetStream = AssetLoader.Open(new Uri("avares://FortnitePorting/Assets/Dependencies/binkadec.exe"));
        if (BinkaFile.GetHash() == assetStream.GetHash()) return;

        var fileStream = new FileStream(BinkaFile.FullName, FileMode.Create, FileAccess.Write);
        await assetStream.CopyToAsync(fileStream);
    }
}