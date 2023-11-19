using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform;
using FortnitePorting.Application;
using Ionic.Zip;

namespace FortnitePorting.Services;

public static class DependencyService
{
    public static readonly FileInfo BinkadecFile = new(Path.Combine(App.DataFolder.FullName, "binkadec.exe"));
    public static readonly FileInfo FFmpegFile = new(Path.Combine(App.DataFolder.FullName, "ffmpeg.exe"));
    private static readonly FileInfo FFmpegZipFile = new(Path.Combine(App.DataFolder.FullName, "ffmpeg.zip"));

    public static void EnsureDependencies()
    {
        TaskService.Run(EnsureBinkadec);
        TaskService.Run(EnsureFFmpeg);
    }

    public static async Task EnsureBinkadec()
    {
        if (BinkadecFile.Exists) return;

        var assetStream = AssetLoader.Open(new Uri("avares://FortnitePorting/Assets/Dependencies/binkadec.exe"));
        var fileStream = new FileStream(BinkadecFile.FullName, FileMode.Create, FileAccess.Write);
        await assetStream.CopyToAsync(fileStream);
    }

    public static async Task EnsureFFmpeg()
    {
        if (FFmpegFile.Exists) return;

        var file = await EndpointService.DownloadFileAsync("https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip", FFmpegZipFile.FullName);
        if (!FFmpegZipFile.Exists) return;
        if (file.Length <= 0) return;

        var zip = ZipFile.Read(FFmpegZipFile.FullName);
        foreach (var zipFile in zip)
        {
            if (!zipFile.FileName.EndsWith("/bin/ffmpeg.exe", StringComparison.OrdinalIgnoreCase)) continue;
            zipFile.Extract(new FileStream(FFmpegFile.FullName, FileMode.OpenOrCreate, FileAccess.Write));
        }

        file.Delete();
    }
}