using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.AssetRegistry;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Writers;
using CUE4Parse.Utils;
using EpicManifestParser.Objects;
using FortnitePorting.AppUtils;
using FortnitePorting.Bundles;
using FortnitePorting.Exports;
using FortnitePorting.Services;
using FortnitePorting.Services.Endpoints.Models;
using FortnitePorting.Views.Controls;
using FortnitePorting.Views.Extensions;

namespace FortnitePorting.ViewModels;

public class CUE4ParseViewModel : ObservableObject
{
    public Manifest? FortniteLiveManifest;
    public UTexture2D? PlaceholderTexture;
    public MusicQueueItem? PlaceholderMusicPack;
    public List<UAnimMontage> MaleIdleAnimations = new();
    public List<UAnimMontage> FemaleIdleAnimations = new();
    public HashSet<string> MeshEntries;

    public readonly FortnitePortingFileProvider Provider;

    public readonly List<FAssetData> AssetDataBuffers = new();

    public readonly RarityCollection[] RarityData = new RarityCollection[8];

    public static readonly VersionContainer Version = new(EGame.GAME_UE5_5);

    private BackupAPI? BackupInfo;

    private static readonly string[] MeshRemoveList =
    {
        "/Sounds",
        "/Playsets",
        "/UI",
        "/2dAssets",
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
        "/_Verse",

        "/PPID_",
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
    
    private static readonly string[] MeshKeepList =
    {
        "/T_M"
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
        if (installType is EInstallType.Local or EInstallType.Custom && !Directory.Exists(directory))
        {
            AppVM.Warning("Installation Not Found", "Fortnite installation path does not exist or has not been set. Please go to settings to verify you've set the right path and restart. The program will not work properly on Local Installation mode if you do not set it.");
            return;
        }

        Provider = installType switch
        {
            EInstallType.Local => new FortnitePortingFileProvider(directory, Version),
            EInstallType.Live => new FortnitePortingFileProvider(Version),
            EInstallType.Custom => new FortnitePortingFileProvider(directory, new VersionContainer(AppSettings.Current.GameVersion))
        };

        Provider.Versions.Options["SkeletalMesh.KeepMobileMinLODSettingOnDesktop"] = true;
        Provider.Versions.Options["StaticMesh.KeepMobileMinLODSettingOnDesktop"] = true;
    }

    public async Task Initialize()
    {
        if (Provider is null) return;

        var oodlePath = Path.Combine(App.DataFolder.FullName, OodleHelper.OODLE_DLL_NAME);
        if (!File.Exists(oodlePath)) await OodleHelper.DownloadOodleDllAsync(oodlePath);
        OodleHelper.Initialize(oodlePath);
        
        BackupInfo = await EndpointService.FortnitePorting.GetBackupAsync();

        AppVM.LoadingVM.Update("Loading Archive");
        await InitializeProvider();
        AppVM.LoadingVM.Update("Initializing Content Builds");
        var contentBuildsInitialized = await InitializeOnDemandFiles();
        if (!contentBuildsInitialized)
        {
            Log.Warning("Failed to load content bundles, textures may be lower resolution than usual");
        }
        await InitializeKeys();
        AppVM.LoadingVM.Update("Loading Mappings");
        await InitializeMappings();
        if (Provider.MappingsContainer is null)
        {
            Log.Warning("Failed to load mappings, issues may occur");
        }

        Provider.LoadLocalization(AppSettings.Current.Language);
        Provider.LoadVirtualPaths();

        AppVM.LoadingVM.Update("Loading Asset Registry");
        var assetRegistries = Provider.Files.Where(x => x.Key.Contains("AssetRegistry", StringComparison.OrdinalIgnoreCase)).ToList();
        assetRegistries.MoveToEnd(x => x.Value.Path.EndsWith("AssetRegistry.bin")); // i want encrypted cosmetics to be at the top :)))
        foreach (var (_, file) in assetRegistries)
        {
            if (file.Path.Contains("UEFN", StringComparison.OrdinalIgnoreCase) || file.Path.Contains("Editor", StringComparison.OrdinalIgnoreCase)) continue;
            await LoadAssetRegistry(file);
        }

        if (assetRegistries.Count == 0)
        {
            Log.Warning("Failed to load asset registry, please ensure your game is up to date");
        }

        AppVM.LoadingVM.Update("Loading Required Assets");
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

        var musicPackObject = await Provider.TryLoadObjectAsync("FortniteGame/Content/Athena/Items/Cosmetics/MusicPacks/MusicPack_000_Default");
        if (musicPackObject is not null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var musicPackAsset = new AssetSelectorItem(musicPackObject, musicPackObject.GetOrDefault<UTexture2D>("SmallPreviewImage", PlaceholderTexture), EAssetType.Music);
                PlaceholderMusicPack = new MusicQueueItem(musicPackAsset.Asset, musicPackAsset.FullSource, "No Music Pack Playing", "Add a Music Pack to the queue to begin listening!");
            }, DispatcherPriority.Background);
        }

        AppVM.LoadingVM.Update("Preloading Mesh Entries");
        var allEntries = AppVM.CUE4ParseVM.Provider.Files.ToArray();
        var removeEntries = AppVM.CUE4ParseVM.AssetDataBuffers.Select(x => AppVM.CUE4ParseVM.Provider.FixPath(x.ObjectPath) + ".uasset").ToHashSet();

        MeshEntries = new HashSet<string>();
        for (var idx = 0; idx < allEntries.Length; idx++)
        {
            var entry = allEntries[idx];
            if (!entry.Key.EndsWith(".uasset") || entry.Key.EndsWith(".o.uasset")) continue;
            if (MeshRemoveList.Any(x => entry.Key.Contains(x, StringComparison.OrdinalIgnoreCase)) 
                && !MeshKeepList.Any(x => entry.Key.Contains(x, StringComparison.OrdinalIgnoreCase))) continue;
            if (removeEntries.Contains(entry.Key)) continue;

            MeshEntries.Add(entry.Value.Path);
        }
    }

    private async Task InitializeProvider()
    {
        switch (AppSettings.Current.InstallType)
        {
            case EInstallType.Local:
            case EInstallType.Custom:
            {
                Provider.Initialize();
                break;
            }
            case EInstallType.Live:
            {
                await LoadFortniteLiveManifest(verbose: true);
                var pakAndUtocFiles = FortniteLiveManifest?.FileManifests.Where(fileManifest => FortniteLiveRegex.IsMatch(fileManifest.Name));
                foreach (var fileManifest in pakAndUtocFiles)
                {
                    Provider.RegisterVfs(fileManifest.Name, new Stream[] { fileManifest.GetStream() }, it => new FStreamArchive(it, FortniteLiveManifest.FileManifests.First(x => x.Name.Equals(it)).GetStream(), Provider.Versions));
                }

                await Provider.MountAsync();

                break;
            }
        }
    }

    private async Task<bool> InitializeOnDemandFiles()
    {
        if (AppSettings.Current.InstallType is EInstallType.Custom)
        {
            return false;
        }

        async Task<string?> GetTocPathAsync(EInstallType type)
        {
            var onDemandString = string.Empty;
            switch (type)
            {
                case EInstallType.Local:
                    var onDemandPath = Path.Combine(AppSettings.Current.ArchivePath, "..\\..\\..\\Cloud\\IoStoreOnDemand.ini");
                    onDemandString = File.Exists(onDemandPath) ? await File.ReadAllTextAsync(onDemandPath) : await GetFortniteLiveTocPathAsync();
                    break;
                case EInstallType.Live:
                    onDemandString = await GetFortniteLiveTocPathAsync();
                    break;
            }

            if (string.IsNullOrEmpty(onDemandString)) return null;

            var onDemandIni = IniReader.Read(onDemandString);
            var tocPath = onDemandIni.Sections["Endpoint"].First(x => x.Name.Equals("TocPath", StringComparison.OrdinalIgnoreCase)).Value.Replace("\"", string.Empty);
            return tocPath;
        }

        var tocPath = await GetTocPathAsync(AppSettings.Current.InstallType);
        tocPath ??= await GetTocPathAsync(EInstallType.Live); // in case of a BuildInfo label mismatch, live is always correct
        if (tocPath is null) return false;

        await DownloadIoStoreOnDemand(tocPath.SubstringAfterLast("/").SubstringBeforeLast("."));

        return true;
    }

    public async Task DownloadIoStoreOnDemand(string onDemandHash)
    {
        var onDemandBytes = await EndpointService.Epic.GetWithAuth($"https://download.epicgames.com/ias/fortnite/{onDemandHash}.iochunktoc");
        if (onDemandBytes is null) return;

        try
        {
            await Provider.RegisterVfs(new IoChunkToc(new FByteArchive("OnDemandToc", onDemandBytes)),
                new IoStoreOnDemandOptions
                {
                    ChunkBaseUri = new Uri("https://download.epicgames.com/ias/fortnite/", UriKind.Absolute),
                    ChunkCacheDirectory = App.CacheFolder,
                    Authorization = new AuthenticationHeaderValue("Bearer", AppSettings.Current.EpicAuth.AccessToken),
                    Timeout = TimeSpan.FromSeconds(100)
                });
            await Provider.MountAsync();
        }
        catch (Exception e)
        {
            Log.Error("Failed to load on-demand cosmetic streaming");
            Log.Error(e.Message + e.StackTrace);
        }

    }
    
    

    private static async Task<string?> GetFortniteLiveTocPathAsync()
    {
        await AppVM.CUE4ParseVM.LoadFortniteLiveManifest();
        var buildInfoFile = AppVM.CUE4ParseVM.FortniteLiveManifest?.FileManifests.FirstOrDefault(x => x.Name.Equals("Cloud/IoStoreOnDemand.ini", StringComparison.OrdinalIgnoreCase));
        if (buildInfoFile is null) return null;

        var stream = buildInfoFile.GetStream();
        var bytes = stream.ToBytes();
        return Encoding.UTF8.GetString(bytes);
    }

    public async Task LoadFortniteLiveManifest(bool verbose = false)
    {
        if (FortniteLiveManifest is not null) return;
        var manifestInfo = await EndpointService.Epic.GetManifestInfoAsync();
        if (verbose)
        {
            AppVM.LoadingVM.Update($"Loading Fortnite Live v{manifestInfo.Version.ToString()}");
            Log.Information($"Loading Manifest for Fortnite {manifestInfo.BuildVersion}");
        }

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
        switch (AppSettings.Current.InstallType)
        {
            case EInstallType.Custom:
            {
                var mounted = 0;
                foreach (var vfs in Provider.UnloadedVfs.ToArray())
                {
                    foreach (var key in AppSettings.Current.CustomAesKeys)
                    {
                        mounted += await Provider.SubmitKeyAsync(vfs.EncryptionKeyGuid, new FAesKey(key.Hex));
                    }
                }
                
                if (mounted == 0)
                {
                    Log.Warning("Failed to load game files, please ensure your game is up to date");
                }

                if (!Provider.TryFindGameFile("FortniteGame/AssetRegistry.bin", out _))
                {
                    AppVM.Warning("Failed to Load Asset Registry", "The Asset Registry could not be loaded because there was no AES key submitted that was valid for this version of Fortnite.");
                }
                break;
            }
            case EInstallType.Local:
            case EInstallType.Live:
            {
                var keyResponse = BackupInfo.IsActive ? BackupInfo.AES : await EndpointService.FortniteCentral.GetKeysAsync();
                if (keyResponse is not null) AppSettings.Current.AesResponse = keyResponse;
                else keyResponse = AppSettings.Current.AesResponse;
                if (keyResponse is null) return;

                var mounted = await Provider.SubmitKeyAsync(Globals.ZERO_GUID, new FAesKey(keyResponse.MainKey));
                if (mounted == 0)
                {
                    Log.Warning("Failed to load game files, please ensure your game is up to date");
                }

                if (!Provider.TryFindGameFile("FortniteGame/AssetRegistry.bin", out _))
                {
                    AppVM.Warning("Failed to Load Asset Registry", "The Asset Registry could not be loaded because there was no AES key submitted that was valid for this version of Fortnite.\nIf any updates were released recently, be patient for AES keys to be updated.");
                }

                foreach (var dynamicKey in keyResponse.DynamicKeys)
                {
                    await Provider.SubmitKeyAsync(new FGuid(dynamicKey.GUID), new FAesKey(dynamicKey.Key));
                }

                break;
            }
        }
    }

    private async Task InitializeMappings()
    {
        switch (AppSettings.Current.InstallType)
        {
            case EInstallType.Custom:
            {
                if (string.IsNullOrEmpty(AppSettings.Current.CustomMappingsPath)) return;
                Provider.MappingsContainer = new FileUsmapTypeMappingsProvider(AppSettings.Current.CustomMappingsPath);
                break;
            }
            case EInstallType.Local:
            case EInstallType.Live:
            {
                if (await TryDownloadMappings()) return;

                LoadLocalMappings();
                break;
            }
        }
    }

    private async Task<bool> TryDownloadMappings()
    {
        var mappingsResponse = BackupInfo.IsActive ? BackupInfo.Mappings : await EndpointService.FortniteCentral.GetMappingsAsync();
        if (mappingsResponse is null) return false;
        if (mappingsResponse.Length <= 0) return false;

        var mappings = mappingsResponse.FirstOrDefault();
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
            Log.Information("Loaded Asset Registry: {0}", file.Path);
        }
        catch (Exception)
        {
            Log.Warning($"Failed to load asset registry: {0}", file.Path);
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