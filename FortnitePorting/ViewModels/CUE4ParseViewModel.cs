using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.AssetRegistry;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Pak;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;
using EpicManifestParser;
using EpicManifestParser.UE;
using FortnitePorting.Application;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.CUE4Parse;
using FortnitePorting.Models.Fortnite;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.Views;
using FortnitePorting.Windows;
using Serilog;
using UE4Config.Parsing;
using FGuid = CUE4Parse.UE4.Objects.Core.Misc.FGuid;

namespace FortnitePorting.ViewModels;

public class CUE4ParseViewModel : ViewModelBase
{
    public bool FinishedLoading;
    
    public readonly HybridFileProvider Provider = AppSettings.Current.Installation.CurrentProfile.FortniteVersion switch
    {
        EFortniteVersion.LatestOnDemand => new HybridFileProvider(new VersionContainer(AppSettings.Current.Installation.CurrentProfile.UnrealVersion)),
        EFortniteVersion.LatestInstalled => new HybridFileProvider(AppSettings.Current.Installation.CurrentProfile.ArchiveDirectory, ExtraDirectories, new VersionContainer(LATEST_GAME_VERSION)),
        _ => new HybridFileProvider(AppSettings.Current.Installation.CurrentProfile.ArchiveDirectory, [], new VersionContainer(AppSettings.Current.Installation.CurrentProfile.UnrealVersion)),
    };
    
    public readonly HybridFileProvider OptionalProvider = AppSettings.Current.Installation.CurrentProfile.FortniteVersion switch
    {
        EFortniteVersion.LatestOnDemand => new HybridFileProvider(new VersionContainer(AppSettings.Current.Installation.CurrentProfile.UnrealVersion), isOptionalLoader: true),
        EFortniteVersion.LatestInstalled => new HybridFileProvider(AppSettings.Current.Installation.CurrentProfile.ArchiveDirectory, [], new VersionContainer(LATEST_GAME_VERSION), isOptionalLoader: true),
        _ => new HybridFileProvider(AppSettings.Current.Installation.CurrentProfile.ArchiveDirectory, [], new VersionContainer(AppSettings.Current.Installation.CurrentProfile.UnrealVersion), isOptionalLoader: true),
    };

    public FBuildPatchAppManifest? LiveManifest;
    
    public readonly List<FAssetData> AssetRegistry = [];
    public readonly List<FRarityCollection> RarityColors = [];
    public readonly Dictionary<int, FColor> BeanstalkColors = [];
    public readonly Dictionary<int, FLinearColor> BeanstalkMaterialProps = [];
    public readonly Dictionary<int, FVector> BeanstalkAtlasTextureUVs = [];
    public readonly List<UAnimMontage> MaleLobbyMontages = [];
    public readonly List<UAnimMontage> FemaleLobbyMontages = [];

    private OnlineResponse _onlineStatus;
    
    private static readonly Regex FortniteArchiveRegex = new(@"^FortniteGame(/|\\)Content(/|\\)Paks(/|\\)(pakchunk(?:0|10.*|\w+)-WindowsClient|global)\.(pak|utoc)$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly List<DirectoryInfo> ExtraDirectories = 
    [
        new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortniteGame", "Saved", "PersistentDownloadDir", "GameCustom", "InstalledBundles"))
    ];
    
    private static readonly List<string> MaleLobbyMontagePaths = 
    [
        "FortniteGame/Content/Animation/Game/MainPlayer/Menu/BR/Male_Commando_Idle_01_M",
        "FortniteGame/Content/Animation/Game/MainPlayer/Menu/BR/Male_commando_Idle_2_M"
    ];
    
    private static readonly List<string> FemaleLobbyMontagePaths = 
    [
        "FortniteGame/Content/Animation/Game/MainPlayer/Menu/BR/Female_Commando_Idle_02_Rebirth_Montage",
        "FortniteGame/Content/Animation/Game/MainPlayer/Menu/BR/Female_Commando_Idle_03_Montage"
    ];

    private const EGame LATEST_GAME_VERSION = EGame.GAME_UE5_6;

    public override async Task Initialize()
    {
		ObjectTypeRegistry.RegisterEngine(Assembly.Load("FortnitePorting"));
        ObjectTypeRegistry.RegisterEngine(Assembly.Load("FortnitePorting.Shared"));

        Provider.LoadExtraDirectories = AppSettings.Current.Installation.CurrentProfile.LoadCreativeMaps;
        
        _onlineStatus = await ApiVM.FortnitePorting.GetOnlineStatusAsync();
        
        if (AppSettings.Current.Installation.CurrentProfile.FortniteVersion is EFortniteVersion.LatestInstalled
            && (_onlineStatus.Backup.Keys 
                ? await ApiVM.FortnitePorting.GetKeysAsync() 
                : await ApiVM.FortniteCentral.GetKeysAsync() ?? await ApiVM.FortnitePorting.GetKeysAsync()) is { } aes
            && !new PakFileReader(Path.Combine(AppSettings.Current.Installation.CurrentProfile.ArchiveDirectory,
                "pakchunk0-WindowsClient.pak")).TestAesKey(new FAesKey(aes.MainKey)))
        {
            await TaskService.RunDispatcherAsync(() =>
            {
                AppWM.TimeWasterOpen = true;
                AppWM.TimeWaster = new TimeWasterView(game: false);
            });
        }

        await CleanupCache();

        Provider.VfsMounted += (sender, _) =>
        {
            if (sender is not IAesVfsReader reader) return;

            HomeVM.UpdateStatus($"Loading {reader.Name}");
        };
        
        OptionalProvider.VfsMounted += (sender, _) =>
        {
            if (sender is not IAesVfsReader reader) return;

            HomeVM.UpdateStatus($"Loading {reader.Name}");
        };
        
        HomeVM.UpdateStatus("Loading Native Libraries");
        await InitializeOodle();
        await InitializeZlib();
        
        HomeVM.UpdateStatus("Loading Game Files");
        await InitializeProvider();
        await InitializeTextureStreaming();
        
        await LoadKeys();
        Provider.LoadLocalization(AppSettings.Current.Installation.CurrentProfile.GameLanguage);
        Provider.LoadVirtualPaths();
        await LoadMappings();
        
        Provider.PostMount();
        
        await LoadAssetRegistries();

        HomeVM.UpdateStatus("Loading Application Assets");
        await LoadApplicationAssets();

        HomeVM.UpdateStatus(string.Empty);

        FinishedLoading = true;
    }
    
    private async Task CleanupCache()
    {
        var files = CacheFolder.GetFiles("*.*chunk");
        var cutoffDate = DateTime.Now - TimeSpan.FromDays(AppSettings.Current.Application.ChunkCacheLifetime);
        foreach (var file in files)
        {
            if (file.LastWriteTime >= cutoffDate) continue;
            
            file.Delete();
        }
    }
    
    private async Task InitializeOodle()
    {
        var oodlePath = Path.Combine(DataFolder.FullName, OodleHelper.OODLE_DLL_NAME);
        if (!File.Exists(oodlePath)) await OodleHelper.DownloadOodleDllAsync(oodlePath);
        OodleHelper.Initialize(oodlePath);
    }
    
    private async Task InitializeZlib()
    {
        var zlibPath = Path.Combine(DataFolder.FullName, ZlibHelper.DLL_NAME);
        if (!File.Exists(zlibPath)) await ZlibHelper.DownloadDllAsync(zlibPath);
        ZlibHelper.Initialize(zlibPath);
    }
    
    private async Task InitializeProvider()
    {
        if (AppSettings.Current.Installation.CurrentProfile.FortniteVersion is EFortniteVersion.LatestInstalled or EFortniteVersion.LatestOnDemand)
        {
            await ApiVM.EpicGames.VerifyAuthAsync();
        }
        
        switch (AppSettings.Current.Installation.CurrentProfile.FortniteVersion)
        {
            case EFortniteVersion.LatestOnDemand:
            {
                var manifestInfo = await ApiVM.EpicGames.GetManifestInfoAsync();
                if (manifestInfo is null) break;

                var options = new ManifestParseOptions
                {
                    ChunkBaseUrl = "http://epicgames-download1.akamaized.net/Builds/Fortnite/CloudDir/",
                    ChunkCacheDirectory = CacheFolder.FullName,
                    ManifestCacheDirectory = CacheFolder.FullName,
                    Decompressor = ManifestZlibStreamDecompressor.Decompress,
                    DecompressorState = ZlibHelper.Instance,
                    CacheChunksAsIs = true
                };
                
                var (manifest, element) = await manifestInfo.DownloadAndParseAsync(options);
                LiveManifest = manifest;

                HomeVM.UpdateStatus($"Loading Fortnite On-Demand (This may take a while)");

                var fileManifests = manifest.Files.Where(fileManifest => FortniteArchiveRegex.IsMatch(fileManifest.FileName));
                foreach (var fileManifest in fileManifests)
                {
                    Provider.RegisterVfs(fileManifest.FileName, (Stream[]) [fileManifest.GetStream()],
                        name => new FStreamArchive(name,
                            manifest.Files.First(file => file.FileName.Equals(name)).GetStream()));
                }
                
                break;
            }
            default:
            {
                Provider.Initialize();
                OptionalProvider.Initialize();
                break;
            }
        }
    }

    private async Task InitializeTextureStreaming()
    {
        if (AppSettings.Current.Installation.CurrentProfile.FortniteVersion is not (EFortniteVersion.LatestInstalled or EFortniteVersion.LatestOnDemand)) return;
        if (AppSettings.Current.Installation.CurrentProfile.FortniteVersion == EFortniteVersion.LatestInstalled && !AppSettings.Current.Installation.CurrentProfile.TextureStreamingEnabled) return;

        try
        {
            var tocPath = await GetTocPath(AppSettings.Current.Installation.CurrentProfile.FortniteVersion);
            if (string.IsNullOrEmpty(tocPath)) return;

            var tocName = tocPath.SubstringAfterLast("/");
            var onDemandFile = new FileInfo(Path.Combine(DataFolder.FullName, tocName));
            if (!onDemandFile.Exists || onDemandFile.Length == 0)
            {
                await ApiVM.DownloadFileAsync($"https://download.epicgames.com/{tocPath}", onDemandFile.FullName);
            }

            var options = new IoStoreOnDemandOptions
            {
                ChunkBaseUri = new Uri("https://download.epicgames.com/ias/fortnite/", UriKind.Absolute),
                ChunkCacheDirectory = CacheFolder,
                Authorization = new AuthenticationHeaderValue("Bearer", AppSettings.Current.Online.EpicAuth?.Token),
                Timeout = TimeSpan.FromSeconds(30)
            };

            var chunkToc = new IoChunkToc(onDemandFile);
            await Provider.RegisterVfs(chunkToc, options);
            await Provider.MountAsync();
        }
        catch (Exception e)
        {
            AppWM.Dialog("Failed to Initialize Texture Streaming", 
                $"Please enable the \"Pre-Download Streamed Assets\" option for Fortnite in the Epic Games Launcher and disable texture streaming in installation settings to remove this popup\n\nException: {e}");
        }
    }
    
    private async Task<string> GetTocPath(EFortniteVersion loadingType)
    {
        var onDemandText = string.Empty;
        switch (loadingType)
        {
            case EFortniteVersion.LatestInstalled:
            {
                var onDemandPath = Path.Combine(AppSettings.Current.Installation.CurrentProfile.ArchiveDirectory, @"..\..\..\Cloud\IoStoreOnDemand.ini");
                if (File.Exists(onDemandPath)) onDemandText = await File.ReadAllTextAsync(onDemandPath);
                break;
            }
            case EFortniteVersion.LatestOnDemand:
            {
                var onDemandFile = LiveManifest?.Files.FirstOrDefault(x => x.FileName.Equals("Cloud/IoStoreOnDemand.ini", StringComparison.OrdinalIgnoreCase));
                if (onDemandFile is not null) onDemandText = onDemandFile.GetStream().ReadToEnd().BytesToString();
                break;
            }
        }

        if (string.IsNullOrEmpty(onDemandText)) return string.Empty;

        var onDemandIni = new ConfigIni();
        onDemandIni.Read(new StringReader(onDemandText));
        return onDemandIni
            .Sections.FirstOrDefault(section => section.Name?.Equals("Endpoint") ?? false)?
            .Tokens.OfType<InstructionToken>().FirstOrDefault(token => token.Key.Equals("TocPath"))?
            .Value.Replace("\"", string.Empty) ?? string.Empty;
    }

    private async Task LoadKeys()
    {
        switch (AppSettings.Current.Installation.CurrentProfile.FortniteVersion)
        {
            case EFortniteVersion.LatestInstalled:
            case EFortniteVersion.LatestOnDemand:
            {
                var aes = _onlineStatus.Backup.Keys 
                    ? await ApiVM.FortnitePorting.GetKeysAsync() 
                    : await ApiVM.FortniteCentral.GetKeysAsync() ?? await ApiVM.FortnitePorting.GetKeysAsync();
                
                if (aes is null)
                {
                    await LoadLocalKeys();
                    break;
                }

                AppSettings.Current.Installation.CurrentProfile.MainKey = new FileEncryptionKey(aes.MainKey);
                await Provider.SubmitKeyAsync(Globals.ZERO_GUID, new FAesKey(aes.MainKey));
                await OptionalProvider.SubmitKeyAsync(Globals.ZERO_GUID, new FAesKey(aes.MainKey));
                
                AppSettings.Current.Installation.CurrentProfile.ExtraKeys.Clear();
                foreach (var key in aes.DynamicKeys)
                {
                    AppSettings.Current.Installation.CurrentProfile.ExtraKeys.Add(new FileEncryptionKey(key.Key));
                    await Provider.SubmitKeyAsync(new FGuid(key.GUID), new FAesKey(key.Key));
                    await OptionalProvider.SubmitKeyAsync(new FGuid(key.GUID), new FAesKey(key.Key));
                }
                
                break;
            }
            default:
            {
                await LoadLocalKeys();
                break;
            }
        }
    }
    
    private async Task LoadLocalKeys()
    {
        var mainKey = AppSettings.Current.Installation.CurrentProfile.MainKey;
        if (mainKey.IsEmpty) mainKey = FileEncryptionKey.Empty;
                
        await Provider.SubmitKeyAsync(Globals.ZERO_GUID, mainKey.EncryptionKey);
        await OptionalProvider.SubmitKeyAsync(Globals.ZERO_GUID, mainKey.EncryptionKey);
                
        foreach (var vfs in Provider.UnloadedVfs.ToArray())
        {
            foreach (var extraKey in AppSettings.Current.Installation.CurrentProfile.ExtraKeys)
            {
                if (extraKey.IsEmpty) continue;
                if (!vfs.TestAesKey(extraKey.EncryptionKey)) continue;
                        
                await Provider.SubmitKeyAsync(vfs.EncryptionKeyGuid, extraKey.EncryptionKey);
                await OptionalProvider.SubmitKeyAsync(vfs.EncryptionKeyGuid, extraKey.EncryptionKey);
            }
        }
    }
    
    private async Task LoadMappings()
    {
        var mappingsPath = AppSettings.Current.Installation.CurrentProfile.FortniteVersion switch
        {
            EFortniteVersion.LatestInstalled or EFortniteVersion.LatestOnDemand => await GetEndpointMappings() ?? GetLocalMappings(),
            _ when AppSettings.Current.Installation.CurrentProfile.UseMappingsFile && File.Exists(AppSettings.Current.Installation.CurrentProfile.MappingsFile) => AppSettings.Current.Installation.CurrentProfile.MappingsFile,
            _ => string.Empty
        };
        
        if (string.IsNullOrEmpty(mappingsPath)) return;
        
        Provider.MappingsContainer = new FileUsmapTypeMappingsProvider(mappingsPath);
        OptionalProvider.MappingsContainer = new FileUsmapTypeMappingsProvider(mappingsPath);
        Log.Information("Loaded Mappings: {Path}", mappingsPath);
    }
    
    private async Task<string?> GetEndpointMappings()
    {
        async Task<string?> GetMappings(Func<string, Task<MappingsResponse[]?>> mappingsFunc)
        {
            var mappings = await mappingsFunc(string.Empty);
            if (mappings is null) return null;
            if (mappings.Length <= 0) return null;

            var foundMappings = mappings.FirstOrDefault();
            if (foundMappings is null) return null;

            var mappingsFilePath = Path.Combine(DataFolder.FullName, foundMappings.Filename);
            if (File.Exists(mappingsFilePath)) return mappingsFilePath;

            var createdFile = await ApiVM.DownloadFileAsync(foundMappings.URL, mappingsFilePath);
            if (createdFile is null) return null;
            
            File.SetCreationTime(mappingsFilePath, foundMappings.Uploaded);

            return mappingsFilePath;
        }
        
        
        return  _onlineStatus.Backup.Mappings
        ? await GetMappings(ApiVM.FortnitePorting.GetMappingsAsync)
        : await GetMappings(ApiVM.FortniteCentral.GetMappingsAsync) ?? await GetMappings(ApiVM.FortnitePorting.GetMappingsAsync);
    }


    private string? GetLocalMappings()
    {
        var usmapFiles = DataFolder.GetFiles("*.usmap");
        if (usmapFiles.Length <= 0) return null;

        var latestUsmap = usmapFiles.MaxBy(x => x.CreationTime);
        return latestUsmap?.FullName;
    }
    
    private async Task LoadAssetRegistries()
    {
        var assetRegistries = Provider.Files
            .Where(x => x.Key.Contains("AssetRegistry", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        
        foreach (var (path, file) in assetRegistries)
        {
            if (!path.EndsWith(".bin")) continue;
            if (path.Contains("Plugin", StringComparison.OrdinalIgnoreCase) || path.Contains("Editor", StringComparison.OrdinalIgnoreCase)) continue;

            HomeVM.UpdateStatus($"Loading {file.Name}");
            var assetArchive = await file.TryCreateReaderAsync();
            if (assetArchive is null) continue;

            try
            {
                var assetRegistry = new FAssetRegistryState(assetArchive);
                AssetRegistry.AddRange(assetRegistry.PreallocatedAssetDataBuffers);
                Log.Information("Loaded Asset Registry: {FilePath}", file.Path);
            }
            catch (Exception e)
            {
                Log.Warning("Failed to load asset registry: {FilePath}", file.Path);
                Log.Error(e.ToString());
            }
        }
    }
    
    private async Task LoadApplicationAssets()
    {
        if (await Provider.TryLoadObjectAsync("FortniteGame/Content/Balance/RarityData") is { } rarityData)
        {
            for (var i = 0; i < rarityData.Properties.Count; i++)
                RarityColors.Add(rarityData.GetByIndex<FRarityCollection>(i));
        }

        if (await Provider.TryLoadObjectAsync("/BeanstalkCosmetics/Cosmetics/DataTables/DT_BeanstalkCosmetics_Colors") is UDataTable beanstalkColorTable)
        {
            foreach (var (name, fallback) in beanstalkColorTable.RowMap)
            {
                var index = int.Parse(name.Text);
                BeanstalkColors[index] = fallback.GetOrDefault<FColor>("Color");
            }
        }
        
        if (await Provider.TryLoadObjectAsync("/BeanstalkCosmetics/Cosmetics/DataTables/DT_BeanstalkCosmetics_MaterialTypes") is UDataTable beanstalkMaterialTypesTable)
        {
            foreach (var (name, fallback) in beanstalkMaterialTypesTable.RowMap)
            {
                var index = int.Parse(name.Text);
                var color = new FLinearColor();
                foreach (var property in fallback.Properties)
                {
                    if (property.Tag is null) continue;
                    
                    var actualName = property.Name.Text.SubstringBefore("_");
                    switch (actualName)
                    {
                        case "Metallic":
                        {
                            color.R = (float) property.Tag.GetValue<double>();
                            break;
                        }
                        case "Roughness":
                        {
                            color.G = (float) property.Tag.GetValue<double>();
                            break;
                        }
                        case "Emissive":
                        {
                            color.B = (float) property.Tag.GetValue<double>();
                            break;
                        }
                    }
                }
                
                BeanstalkMaterialProps[index] = color;
            }
        }
        
        if (await Provider.TryLoadObjectAsync("/BeanstalkCosmetics/Cosmetics/DataTables/DT_PatternAtlasTextureSlots") is UDataTable beanstalkAtlasSlotsTable)
        {
            foreach (var (name, fallback) in beanstalkAtlasSlotsTable.RowMap)
            {
                var index = int.Parse(name.Text);
                foreach (var property in fallback.Properties)
                {
                    if (property.Tag is null) continue;
                    
                    var actualName = property.Name.Text.SubstringBefore("_");
                    if (!actualName.Equals("UV")) continue;
                    
                    BeanstalkAtlasTextureUVs[index] = property.Tag.GetValue<FVector>();
                }
            }
        }
        
        foreach (var path in MaleLobbyMontagePaths)
        {
            MaleLobbyMontages.AddIfNotNull(await Provider.TryLoadObjectAsync<UAnimMontage>(path));
        }
        
        foreach (var path in FemaleLobbyMontagePaths)
        {
            FemaleLobbyMontages.AddIfNotNull(await Provider.TryLoadObjectAsync<UAnimMontage>(path));
        }
    }
}