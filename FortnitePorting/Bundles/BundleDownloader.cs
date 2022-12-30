using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CUE4Parse.UE4.Readers;
using EpicManifestParser.Objects;
using FortnitePorting.AppUtils;
using FortnitePorting.Services;
using FortnitePorting.Views.Extensions;
using RestSharp;

namespace FortnitePorting.Bundles;

public static class BundleDownloader
{
    public const string MANIFEST_URL = "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/Windows/5cb97847cee34581afdbc445400e2f77/FortniteContentBuilds";
    private const string BUNDLE_MAPPINGS_PATH = "FortniteGame/Config/Windows/CosmeticBundleMapping.ini";

    private static Ini CosmeticBundleMappings;
    private static Manifest? BundleManifest;

    public static async Task<bool> Initialize()
    {
        var bytes = await AppVM.CUE4ParseVM.Provider.TrySaveAssetAsync(BUNDLE_MAPPINGS_PATH);
        if (bytes is null) return false;

        var bundleString = Encoding.UTF8.GetString(bytes);
        CosmeticBundleMappings = BundleIniReader.Read(bundleString);

        BundleManifest = await GetManifest();
        return true;
    }

    private static async Task<Manifest?> GetManifest()
    {
        var buildInfoString = string.Empty;
        switch (AppSettings.Current.InstallType)
        {
            case EInstallType.Local:
            {
                var buildInfoPath = Path.Combine(AppSettings.Current.ArchivePath, "..\\..\\..\\Cloud\\BuildInfo.ini");
                buildInfoString = await File.ReadAllTextAsync(buildInfoPath);
                break;
            }
            case EInstallType.Live:
            {
                var buildInfoFile = AppVM.CUE4ParseVM.FortniteLiveManifest?.FileManifests.FirstOrDefault(x => x.Name.Equals("Cloud/BuildInfo.ini", StringComparison.OrdinalIgnoreCase));
                if (buildInfoFile is null) return null;

                var stream = buildInfoFile.GetStream();
                var bytes = stream.ToBytes();
                buildInfoString = Encoding.UTF8.GetString(bytes);
                break;
            }
        }

        var buildInfoIni = BundleIniReader.Read(buildInfoString);
        var label = buildInfoIni.Sections["Content"].First(x => x.Name.Equals("Label", StringComparison.OrdinalIgnoreCase)).Value;

        var contentBuilds = await EndpointService.Epic.GetContentBuildsAsync(url: MANIFEST_URL, label: label);
        if (contentBuilds is null) return null;

        var contentManifest = contentBuilds.Items.Manifest;
        var manifestUrl = contentManifest.Distribution + contentManifest.Path;

        return await EndpointService.Epic.GetManifestAsync(url: manifestUrl);
    }

    public static async Task<IEnumerable<FileInfo>> DownloadAsync(string cosmetic)
    {
        if (!AppSettings.Current.BundleDownloaderEnabled) return Enumerable.Empty<FileInfo>();
        if (!CosmeticBundleMappings.Sections.ContainsKey(cosmetic)) return Enumerable.Empty<FileInfo>();
        var cosmeticSection = CosmeticBundleMappings.Sections[cosmetic];
        var sectionBundles = cosmeticSection.Where(x => x.Name.Equals("Bundles")).Select(x => x.Value).ToList();
        var bundles = BundleManifest?.FileManifests.Where(x => sectionBundles.Contains(x.InstallTags[0]));
        if (bundles is null) return Enumerable.Empty<FileInfo>();

        var downloadedBundles = new List<FileInfo>();
        foreach (var bundle in bundles)
        {
            var targetFile = new FileInfo(Path.Combine(App.BundlesFolder.FullName, bundle.InstallTags[0], bundle.Name));
            if (targetFile.Exists) continue;
            Directory.CreateDirectory(targetFile.DirectoryName!);

            Log.Information("Downloading file bundle: {0}", bundle.Name);
            await File.WriteAllBytesAsync(targetFile.FullName, bundle.GetStream().ToBytes());
            downloadedBundles.Add(targetFile);
        }
        return downloadedBundles;
    }

    public static IEnumerable<FileInfo> Download(string cosmetic)
    {
        return DownloadAsync(cosmetic).GetAwaiter().GetResult();
    }
}