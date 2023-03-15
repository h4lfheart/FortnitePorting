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
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
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
    public List<UAnimMontage> MaleIdleAnimations = new();
    public List<UAnimMontage> FemaleIdleAnimations = new();
    public HashSet<string> MeshEntries;

    public readonly FortnitePortingFileProvider Provider;

    public readonly List<FAssetData> AssetDataBuffers = new();

    public readonly RarityCollection[] RarityData = new RarityCollection[8];

    public static readonly VersionContainer Version = new(EGame.GAME_UE5_2);

    private static readonly List<DirectoryInfo> ExtraDirectories = new()
    {
        new DirectoryInfo(App.BundlesFolder.FullName)
    };
    
    private static readonly string[] MeshRemoveList = {
        "/Sounds",
        "/Playsets",
        "/UI",
        "/2dAssets",
        "/Animation",
        "/Textures",
        "/Audio",
        "/Sound",
        "/Materials",
        "/Icons",
        "/Anims",
        "/DataTables",
        "/TextureData",
        "/ActorBlueprints",
        "/Physics",
        
        "/PPID_",
        "/M_",
        "/MI_",
        "/MF_",
        "/NS_",
        "/T_",
        "/P_",
        "/TD_",
        "/MPC_",
        "/BP_",
        
        "Engine/",
        
        "_Physics",
        "_AnimBP",
        "_PhysMat",
        "_PoseAsset",
        
        "PlaysetGrenade",
        "NaniteDisplacement"
    };

    private static readonly Regex FortniteLiveRegex = new(@"^FortniteGame(/|\\)Content(/|\\)Paks(/|\\)(pakchunk(?:0|10.*|\w+)-WindowsClient|global)\.(pak|utoc)$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly string[] MaleIdlePaths =
    {
        "FortniteGame/Content/Animation/Game/MainPlayer/Menu/BR/Male_Commando_Idle_01_M",
        "FortniteGame/Content/Animation/Game/MainPlayer/Menu/BR/Male_commando_Idle_2_M"
    };

    private static readonly string[] FemaleIdlePaths =
    {
        "FortniteGame/Content/Animation/Game/MainPlayer/Menu/BR/Female_Commando_Idle_01_M",
        "FortniteGame/Content/Animation/Game/MainPlayer/Menu/BR/Female_Commando_Idle_02_Rebirth_Montage",
        "FortniteGame/Content/Animation/Game/MainPlayer/Menu/BR/Female_Commando_Idle_03_Rebirth_Montage"
    };

    public CUE4ParseViewModel(string directory, EInstallType installType)
    {
        var narrowedDirectories = ExtraDirectories.Where(x => x.Exists).ToList();
        if (installType == EInstallType.Local && !Directory.Exists(directory))
        {
            AppVM.Warning("Installation Not Found", "Fortnite installation path does not exist or has not been set. Please go to settings to verify you've set the right path and restart. The program will not work properly on Local Installation mode if you do not set it.");
            return;
        }

        Provider = installType switch
        {
            EInstallType.Local => new FortnitePortingFileProvider(new DirectoryInfo(directory), narrowedDirectories, SearchOption.AllDirectories, true, Version),
            EInstallType.Live => new FortnitePortingFileProvider(true, Version),
        };
    }

    public async Task Initialize()
    {
        if (Provider is null) return;

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
            Log.Information("Successfully Initialized Bundle Downloader");
        }
        else
        {
            AppLog.Warning("Failed to initialize Bundle Downloader, high resolution textures will not be downloaded");
        }

        var assetRegistries = Provider.Files.Where(x => x.Key.Contains("AssetRegistry", StringComparison.OrdinalIgnoreCase)).ToList();
        assetRegistries.MoveToEnd(x => x.Value.Name.Equals("AssetRegistry.bin")); // i want encrypted cosmetics to be at the top :)))
        foreach (var (_, file) in assetRegistries)
        {
            await LoadAssetRegistry(file);
        }

        if (assetRegistries.Count == 0)
        {
            AppLog.Warning("Failed to load asset registry, please ensure your game is up to date");
        }

        var rarityData = await Provider.LoadObjectAsync("FortniteGame/Content/Balance/RarityData.RarityData");
        for (var i = 0; i < 8; i++)
        {
            RarityData[i] = rarityData.GetByIndex<RarityCollection>(i);
        }

        PlaceholderTexture = await Provider.TryLoadObjectAsync<UTexture2D>("FortniteGame/Content/Athena/Prototype/Textures/T_Placeholder_Generic");

        foreach (var path in FemaleIdlePaths)
        {
            var montage = await Provider.TryLoadObjectAsync<UAnimMontage>(path);
            if (montage is null) continue;

            FemaleIdleAnimations.Add(montage);
        }

        foreach (var path in MaleIdlePaths)
        {
            var montage = await Provider.TryLoadObjectAsync<UAnimMontage>(path);
            if (montage is null) continue;

            MaleIdleAnimations.Add(montage);
        }
        
        var allEntries = AppVM.CUE4ParseVM.Provider.Files.ToArray();
        var removeEntries = AppVM.CUE4ParseVM.AssetDataBuffers.Select(x => AppVM.CUE4ParseVM.Provider.FixPath(x.ObjectPath) + ".uasset").ToHashSet();

        MeshEntries = new HashSet<string>();
        for (var idx = 0; idx < allEntries.Length; idx++)
        {
            var entry = allEntries[idx];
            if (!entry.Key.EndsWith(".uasset")) continue;
            if (MeshRemoveList.Any(x => entry.Key.Contains(x, StringComparison.OrdinalIgnoreCase))) continue;
            if (removeEntries.Contains(entry.Key)) continue;

            MeshEntries.Add(entry.Value.Path);
        }
        
        
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
                await LoadFortniteLiveManifest(verbose: true);
                var pakAndUtocFiles = FortniteLiveManifest?.FileManifests.Where(fileManifest => FortniteLiveRegex.IsMatch(fileManifest.Name));
                foreach (var fileManifest in pakAndUtocFiles)
                {
                    Provider.Initialize(fileManifest.Name, new Stream[] { fileManifest.GetStream() }, it => new FStreamArchive(it, FortniteLiveManifest.FileManifests.First(x => x.Name.Equals(it)).GetStream(), Provider.Versions));
                }

                break;
            }
        }
    }

    public async Task LoadFortniteLiveManifest(bool verbose = false)
    {
        if (FortniteLiveManifest is not null) return;
        var manifestInfo = await EndpointService.Epic.GetManifestInfoAsync();
        if (verbose) AppLog.Information($"Loading Manifest for Fortnite {manifestInfo.BuildVersion}");

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
    }

    private async Task InitializeKeys()
    {
        var keyResponse = await EndpointService.FortniteCentral.GetKeysAsync();
        if (keyResponse is not null) AppSettings.Current.AesResponse = keyResponse;
        else keyResponse = AppSettings.Current.AesResponse;
        if (keyResponse is null) return;

        var mounted = await Provider.SubmitKeyAsync(Globals.ZERO_GUID, new FAesKey(keyResponse.MainKey));
        if (mounted == 0)
        {
            AppLog.Warning("Failed to load game files, please ensure your game is up to date");
        }

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
            AppLog.Warning($"Failed to load asset registry: {file.Path}");
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