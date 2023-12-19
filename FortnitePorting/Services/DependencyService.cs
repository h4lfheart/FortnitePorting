using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using FortnitePorting.Framework.Services;

namespace FortnitePorting.Services;

public static class DependencyService
{
    public static readonly FileInfo BinkaFile = new(Path.Combine(DataFolder.FullName, "binkadec.exe"));
    public static readonly FileInfo BlenderScriptFile = new(Path.Combine(DataFolder.FullName, "enable_addon.py"));
    public static readonly FileInfo UpdaterFile = new(Path.Combine(DataFolder.FullName, "updater.bat"));

    public static void EnsureDependencies()
    {
        Ensure("Assets/Dependencies/binkadec.exe", BinkaFile);
        Ensure("Plugins/Blender/enable_addon.py", BlenderScriptFile);
        Ensure("Assets/Dependencies/updater.bat", UpdaterFile);
    }

    public static void Ensure(string path, FileInfo targetFile)
    {
        var assetStream = AssetLoader.Open(new Uri($"avares://FortnitePorting/{path}"));
        if (targetFile is { Exists: true, Length: > 0 } && targetFile.GetHash() == assetStream.GetHash()) return;

        targetFile.Delete();
        File.WriteAllBytes(targetFile.FullName, assetStream.ReadToEnd());
    }
}