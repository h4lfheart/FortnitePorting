using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.AssetRegistry;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using EpicManifestParser.Objects;
using FortnitePorting.AppUtils;
using FortnitePorting.Bundles;
using FortnitePorting.Services;
using FortnitePorting.Views.Extensions;

namespace FortnitePorting.ViewModels;

public class CUE4ParseViewModel : ObservableObject
{
    public Manifest? FortniteLiveManifest;
    public UTexture2D? PlaceholderTexture;
    
    public readonly FortnitePortingFileProvider Provider;

    public readonly List<FAssetData> AssetDataBuffers = new();
    
    public readonly RarityCollection[] RarityData = new RarityCollection[8];

    public static readonly VersionContainer Version = new(EGame.GAME_UE5_2);

    private static readonly List<DirectoryInfo> ExtraDirectories = new()
    {
        new DirectoryInfo(App.BundlesFolder.FullName)
    };
    
    private static readonly Regex FortniteLiveRegex = new(@"^FortniteGame(/|\\)Content(/|\\)Paks(/|\\)(pakchunk(?:0|10.*|\w+)-WindowsClient|global)\.(pak|utoc)$",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public CUE4ParseViewModel(string directory, EInstallType installType)
    {
        var narrowedDirectories = ExtraDirectories.Where(x => x.Exists).ToList();
        Provider = installType switch
        {
            EInstallType.Local => new FortnitePortingFileProvider(new DirectoryInfo(directory), narrowedDirectories, SearchOption.AllDirectories, true, Version),
            EInstallType.Live => new FortnitePortingFileProvider(true, Version),
        };
    }
    
    public async Task Initialize()
    {
        await InitializeProvider();
        await InitializeKeys();
        await InitializeMappings();
        if (Provider.MappingsContainer is null)
        {
            AppLog.Warning("Failed to load mappings, issues may occur");
        }
        
        Provider.LoadLocalization(AppSettings.Current.Language);
        Provider.LoadVirtualPaths();
        
        var bundleDownloaderSuccess = await BundleDownloader.Initialize();
        if (bundleDownloaderSuccess)
        {
            Log.Information("Successfully initialized Bundle Downloader");
        }
        else
        {
            AppLog.Warning("Failed to initialize Bundle Downloader, high resolution textures will not be downloaded");
        }

        var assetRegistries = Provider.Files.Where(x =>
            x.Key.Contains("AssetRegistry", StringComparison.OrdinalIgnoreCase)).ToList();
        assetRegistries.MoveToEnd(x => x.Value.Name.Equals("AssetRegistry.bin")); // i want encrypted cosmetics to be at the top :)))
        foreach (var (_, file) in assetRegistries)
        {
            await LoadAssetRegistry(file);
        }

        if (assetRegistries.Count == 0)
        {
            AppLog.Warning("Failed to load game files, please ensure your game is up to date");
        }
        
        var rarityData = await Provider.LoadObjectAsync("FortniteGame/Content/Balance/RarityData.RarityData");
        for (var i = 0; i < 8; i++)
        {
            RarityData[i] = rarityData.GetByIndex<RarityCollection>(i);
        }

        PlaceholderTexture =
            await AppVM.CUE4ParseVM.Provider.LoadObjectAsync<UTexture2D>(
                "FortniteGame/Content/Athena/Prototype/Textures/T_Placeholder_Generic");
    }

    private async Task InitializeProvider()
    {
        switch (AppSettings.Current.InstallType)
        {
            case EInstallType.Local:
            {
                Provider.InitializeLocal();
                break;
            }
            case EInstallType.Live:
            {
                var manifestInfo = await EndpointService.Epic.GetManifestInfoAsync();
                AppLog.Information($"Loading Manifest for Fortnite {manifestInfo.BuildVersion}");
                
                var manifestPath = Path.Combine(App.DataFolder.FullName, manifestInfo.FileName);
                byte[] manifestBytes;
                if (File.Exists(manifestPath))
                {
                    manifestBytes = await File.ReadAllBytesAsync(manifestPath);
                }
                else
                {
                    manifestBytes = await manifestInfo.DownloadManifestDataAsync();
                    await File.WriteAllBytesAsync(manifestPath, manifestBytes);
                }
                
                FortniteLiveManifest = new Manifest(manifestBytes, new ManifestOptions
                {
                    ChunkBaseUri = new Uri("https://epicgames-download1.akamaized.net/Builds/Fortnite/CloudDir/ChunksV4/", UriKind.Absolute),
                    ChunkCacheDirectory = App.CacheFolder
                });

                var pakAndUtocFiles = FortniteLiveManifest.FileManifests.Where(fileManifest => FortniteLiveRegex.IsMatch(fileManifest.Name));
                foreach (var fileManifest in pakAndUtocFiles)
                {
                    Provider.Initialize(fileManifest.Name, new Stream[] { fileManifest.GetStream() }, it => new FStreamArchive(it, FortniteLiveManifest.FileManifests.First(x => x.Name.Equals(it)).GetStream(), Provider.Versions));
                }
                break;
            }
            
            
        }
    }

    private async Task InitializeKeys()
    {
        var keyResponse = await EndpointService.FortniteCentral.GetKeysAsync();
        if (keyResponse is not null) AppSettings.Current.AesResponse = keyResponse;
        else keyResponse = AppSettings.Current.AesResponse;
        if (keyResponse is null) return;
        
        await Provider.SubmitKeyAsync(Globals.ZERO_GUID, new FAesKey(keyResponse.MainKey));
        foreach (var dynamicKey in keyResponse.DynamicKeys)
        {
            await Provider.SubmitKeyAsync(new FGuid(dynamicKey.GUID), new FAesKey(dynamicKey.Key));
        }
    }

    private async Task InitializeMappings()
    {
        if (await TryDownloadMappings()) return;
        
        LoadLocalMappings();
    }

    private async Task<bool> TryDownloadMappings()
    {
        var mappingsResponse = await EndpointService.FortniteCentral.GetMappingsAsync();
        if (mappingsResponse is null) return false;
        if (mappingsResponse.Length <= 0) return false;
        
        var mappings = mappingsResponse.FirstOrDefault(x => x.Meta.CompressionMethod.Equals("Oodle", StringComparison.OrdinalIgnoreCase));
        if (mappings is null) return false;
            
        var mappingsFilePath = Path.Combine(App.DataFolder.FullName, mappings.Filename);
        if (File.Exists(mappingsFilePath)) return false;
            
        await EndpointService.DownloadFileAsync(mappings.URL, mappingsFilePath);
        Provider.MappingsContainer = new FileUsmapTypeMappingsProvider(mappingsFilePath);

        return true;
    }

    private void LoadLocalMappings()
    {
        var usmapFiles = App.DataFolder.GetFiles("*.usmap");
        if (usmapFiles.Length <= 0) return;
            
        var latestUsmap = usmapFiles.MaxBy(x => x.LastWriteTime);
        if (latestUsmap is null) return;

        Provider.MappingsContainer = new FileUsmapTypeMappingsProvider(latestUsmap.FullName);
    }

    private async Task LoadAssetRegistry(GameFile file)
    {
        var assetArchive = await file.TryCreateReaderAsync();
        if (assetArchive is null) return;

        try
        {
            var assetRegistry = new FAssetRegistryState(assetArchive);
            AssetDataBuffers.AddRange(assetRegistry.PreallocatedAssetDataBuffers);
        }
        catch (Exception)
        {
            AppLog.Warning($"Failed to load asset registry: {file.Name}");
        }
    }
}

[StructFallback]
public class RarityCollection
{
    public FLinearColor Color1;
    public FLinearColor Color2;
    public FLinearColor Color3;
    public FLinearColor Color4;
    public FLinearColor Color5;
    public float Radius;
    public float Falloff;
    public float Brightness;
    public float Roughness;
    
    public RarityCollection(FStructFallback fallback)
    {
        Color1 = fallback.GetOrDefault<FLinearColor>(nameof(Color1));
        Color2 = fallback.GetOrDefault<FLinearColor>(nameof(Color2));
        Color3 = fallback.GetOrDefault<FLinearColor>(nameof(Color3));
        Color4 = fallback.GetOrDefault<FLinearColor>(nameof(Color4));
        Color5 = fallback.GetOrDefault<FLinearColor>(nameof(Color5));
        
        Radius = fallback.GetOrDefault<float>(nameof(Radius));
        Falloff = fallback.GetOrDefault<float>(nameof(Falloff));
        Brightness = fallback.GetOrDefault<float>(nameof(Brightness));
        Roughness = fallback.GetOrDefault<float>(nameof(Roughness));
    }

}