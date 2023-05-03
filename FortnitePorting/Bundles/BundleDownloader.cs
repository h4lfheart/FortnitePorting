using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.UObject;
using EpicManifestParser.Objects;
using FortnitePorting.AppUtils;
using FortnitePorting.Services;
using FortnitePorting.Views.Extensions;

namespace FortnitePorting.Bundles;

public static class BundleDownloader
{
    public const string MANIFEST_URL = "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/Windows/5cb97847cee34581afdbc445400e2f77/FortniteContentBuilds";

    private static Manifest? BundleManifest;

    public static async Task<bool> Initialize()
    {
        BundleManifest = await GetManifest();
        return BundleManifest is not null;
    }

    private static async Task<Manifest?> GetManifest()
    {
        var buildInfoString = string.Empty;
        switch (AppSettings.Current.InstallType)
        {
            case EInstallType.Local:
                var buildInfoPath = Path.Combine(AppSettings.Current.ArchivePath, "..\\..\\..\\Cloud\\BuildInfo.ini");
                buildInfoString = File.Exists(buildInfoPath) ? await File.ReadAllTextAsync(buildInfoPath) : await GetFortniteLiveBuildInfoAsync();
                break;
            case EInstallType.Live:
                buildInfoString = await GetFortniteLiveBuildInfoAsync();
                break;
        }

        if (string.IsNullOrEmpty(buildInfoString)) return null;

        var buildInfoIni = BundleIniReader.Read(buildInfoString);
        var label = buildInfoIni.Sections["Content"].First(x => x.Name.Equals("Label", StringComparison.OrdinalIgnoreCase)).Value;

        var contentBuilds = await EndpointService.Epic.GetContentBuildsAsync(url: MANIFEST_URL, label: label);
        if (contentBuilds is null) return null;

        var contentManifest = contentBuilds.Items.Manifest;
        var manifestUrl = contentManifest.Distribution + contentManifest.Path;

        return await EndpointService.Epic.GetManifestAsync(url: manifestUrl);
    }

    public static async Task<IEnumerable<FileInfo>> DownloadAsync(UObject asset)
    {
        if (BundleManifest is null) return Enumerable.Empty<FileInfo>();
        if (!AppSettings.Current.BundleDownloaderEnabled) return Enumerable.Empty<FileInfo>();
        if (!asset.TryGetValue(out string bundleName, "DynamicInstallBundleName")) return Enumerable.Empty<FileInfo>();

        var bundles = BundleManifest.FileManifests.Where(x => x.InstallTags.Contains(bundleName + "_Optional") || x.InstallTags.Contains(bundleName)).ToArray();
        if (bundles is null) return Enumerable.Empty<FileInfo>();

        var downloadedBundles = new List<FileInfo>();
        foreach (var bundle in bundles)
        {
            var targetFile = new FileInfo(Path.Combine(App.BundlesFolder.FullName, bundle.Name));
            if (targetFile.Exists) continue;
            Directory.CreateDirectory(targetFile.DirectoryName!);

            Log.Information("Downloading content bundle: {0}", bundle.Name);
            await File.WriteAllBytesAsync(targetFile.FullName, bundle.GetStream().ToBytes());
            downloadedBundles.Add(targetFile);
        }

        return downloadedBundles;
    }

    public static IEnumerable<FileInfo> Download(UObject asset)
    {
        return DownloadAsync(asset).GetAwaiter().GetResult();
    }

    private static async Task<string?> GetFortniteLiveBuildInfoAsync()
    {
        await AppVM.CUE4ParseVM.LoadFortniteLiveManifest();
        var buildInfoFile = AppVM.CUE4ParseVM.FortniteLiveManifest?.FileManifests.FirstOrDefault(x => x.Name.Equals("Cloud/BuildInfo.ini", StringComparison.OrdinalIgnoreCase));
        if (buildInfoFile is null) return null;

        var stream = buildInfoFile.GetStream();
        var bytes = stream.ToBytes();
        return Encoding.UTF8.GetString(bytes);
    }

    private static async Task<string?> GetFortniteLiveBuildInfo()
    {
        return GetFortniteLiveBuildInfoAsync().GetAwaiter().GetResult();
    }
}