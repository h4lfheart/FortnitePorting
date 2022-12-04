using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.AssetRegistry;
using CUE4Parse.UE4.AssetRegistry.Objects;
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
    public readonly AbstractVfsFileProvider Provider;

    public List<FAssetData> AssetDataBuffers = new();
    
    public RarityCollection[] RarityData = new RarityCollection[8];
    
    private static readonly Regex FortniteLiveRegex = new(@"^FortniteGame(/|\\)Content(/|\\)Paks(/|\\)(pakchunk(?:0|10.*|\w+)-WindowsClient|global)\.(pak|utoc)$",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public CUE4ParseViewModel(string directory, EInstallType installType)
    {
        Provider = installType switch
        {
            EInstallType.Local => new DefaultFileProvider(directory, SearchOption.TopDirectoryOnly, isCaseInsensitive: true, new VersionContainer(EGame.GAME_UE5_1)),
            EInstallType.Live => new StreamedFileProvider("FortniteLive", true, new VersionContainer(EGame.GAME_UE5_1))
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
        
        var bundleStatus = await BundleDownloader.Initialize();
        if (!bundleStatus)
        {
            AppLog.Warning("Failed to initialize Bundle Downloader, HD textures will not be downloaded");
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
    }

    private async Task InitializeProvider()
    {
        switch (Provider)
        {
            case DefaultFileProvider defaultProvider:
            {
                defaultProvider.Initialize();
                break;
            }
            case StreamedFileProvider streamedProvider:
            {
                var manifestInfo = await EndpointService.Epic.GetMainfestAsync();
                AppLog.Information($"Loading manifest for version {manifestInfo.BuildVersion}, this may take a while");
                
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
                
                var manifest = new Manifest(manifestBytes, new ManifestOptions
                {
                    ChunkBaseUri = new Uri("https://epicgames-download1.akamaized.net/Builds/Fortnite/CloudDir/ChunksV4/", UriKind.Absolute),
                    ChunkCacheDirectory = App.CacheFolder
                });

                var pakAndUtocFiles = manifest.FileManifests.Where(fileManifest => FortniteLiveRegex.IsMatch(fileManifest.Name));
                foreach (var fileManifest in pakAndUtocFiles)
                {
                    streamedProvider.Initialize(fileManifest.Name, new Stream[] { fileManifest.GetStream() }, it => new FStreamArchive(it, manifest.FileManifests.First(x => x.Name.Equals(it)).GetStream(), streamedProvider.Versions));
                }
                break;
            }
            
            
        }
    }

    private async Task InitializeKeys()
    {
        var keyResponse = await EndpointService.FortniteCentral.GetKeysAsync();
        if (keyResponse is not null) AppSettings.Current.AesResponse = keyResponse;
        keyResponse ??= AppSettings.Current.AesResponse;
        
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
                
        var assetRegistry = new FAssetRegistryState(assetArchive);
        AssetDataBuffers.AddRange(assetRegistry.PreallocatedAssetDataBuffers);
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