using System.Text;
using System.Threading.Tasks;
using EpicManifestParser.Objects;

namespace FortnitePorting.Bundles;

public static class BundleDownloader
{
    private const string BUNDLE_MAPPINGS_PATH = "FortniteGame/Config/Windows/CosmeticBundleMapping.ini";

    private static Ini CosmeticBundleMappings;
    private static Manifest BundleManifest;
    public static async Task<bool> Initialize()
    {
        var bytes = await AppVM.CUE4ParseVM.Provider.TrySaveAssetAsync(BUNDLE_MAPPINGS_PATH);
        if (bytes is null) return false;
        
        var bundleString = Encoding.UTF8.GetString(bytes);
        CosmeticBundleMappings = BundleIniReader.Read(bundleString);

        return true;
    }

    private static Manifest? GetManifest()
    {
        //TODO ADD SPLIT FOR LIVE VS LOCAL
        return null;
    }
}