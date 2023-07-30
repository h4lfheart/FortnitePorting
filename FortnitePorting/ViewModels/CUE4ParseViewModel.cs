using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
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
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using EpicManifestParser.Objects;
using FortnitePorting.AppUtils;
using FortnitePorting.Bundles;
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

    public static readonly VersionContainer Version = new(EGame.GAME_UE5_3);

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
            EInstallType.Local => new FortnitePortingFileProvider(new DirectoryInfo(directory), SearchOption.TopDirectoryOnly, true, Version),
            EInstallType.Live => new FortnitePortingFileProvider(true, Version),
            EInstallType.Custom => new FortnitePortingFileProvider(new DirectoryInfo(directory), SearchOption.TopDirectoryOnly, true, new VersionContainer(AppSettings.Current.GameVersion))
        };

        Provider.Versions.Options["SkeletalMesh.KeepMobileMinLODSettingOnDesktop"] = true;
        Provider.Versions.Options["StaticMesh.KeepMobileMinLODSettingOnDesktop"] = true;
    }

    public async Task Initialize()
    {
        if (Provider is null) return;

        BackupInfo = await EndpointService.FortnitePorting.GetBackupAsync();

        AppVM.LoadingVM.Update("Loading Archive");
        await InitializeProvider();
        await InitializeKeys();
        AppVM.LoadingVM.Update("Loading Mappings");
        await InitializeMappings();
        if (Provider.MappingsContainer is null)
        {
            Log.Warning("Failed to load mappings, issues may occur");
        }

        AppVM.LoadingVM.Update("Initializing Content Builds");
        var contentBuildsInitialized = await InitializeContentBuilds();
        if (!contentBuildsInitialized)
        {
            Log.Warning("Failed to load content bundles, textures may be lower resolution than usual");
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
            case EInstallType.Custom:
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

    private async Task<bool> InitializeContentBuilds()
    {
        if (AppSettings.Current.InstallType is EInstallType.Custom)
        {
            return false;
        }

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

        if (string.IsNullOrEmpty(buildInfoString)) return false;

        var buildInfoIni = BundleIniReader.Read(buildInfoString);
        var label = buildInfoIni.Sections["Content"].First(x => x.Name.Equals("Label", StringComparison.OrdinalIgnoreCase)).Value;

        var contentBuilds = await EndpointService.Epic.GetContentBuildsAsync(label: label);
        if (contentBuilds is null) return false;

        var contentManifest = contentBuilds.Items.Manifest;
        var manifestUrl = contentManifest.Distribution + contentManifest.Path;

        var manifest = await EndpointService.Epic.GetManifestAsync(url: manifestUrl);
        var contentBuildFiles = new Dictionary<string, GameFile>();
        foreach (var fileManifest in manifest.FileManifests)
        {
            if (Provider.Files.ContainsKey(fileManifest.Name)) continue;

            var streamedFile = new StreamedGameFile(fileManifest.Name, fileManifest.GetStream(), Provider.Versions);
            var path = streamedFile.Path.ToLowerInvariant();
            contentBuildFiles[path] = streamedFile;
        }

        var fileDict = (FileProviderDictionary) Provider.Files;
        fileDict.AddFiles(contentBuildFiles);

        return true;
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

    public async Task LoadFortniteLiveManifest(bool verbose = false)
    {
        if (FortniteLiveManifest is not null) return;
        var manifestInfo = await EndpointService.Epic.GetManifestInfoAsync();
        AppVM.LoadingVM.Update($"Loading Fortnite Live v{manifestInfo.Version.ToString()}");
        if (verbose) Log.Information($"Loading Manifest for Fortnite {manifestInfo.BuildVersion}");

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
                foreach (var vfs in Provider.UnloadedVfs.ToArray())
                {
                    foreach (var key in AppSettings.Current.CustomAesKeys)
                    {
                        await Provider.SubmitKeyAsync(vfs.EncryptionKeyGuid, new FAesKey(key.Hex));
                    }
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
                    AppVM.Warning("Fortnite Update", "Your Fortnite installation is not up to date. Please update to the latest version for FortnitePorting to work properly.");
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