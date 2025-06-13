using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion.Textures;
using CUE4Parse_Conversion.Textures.BC;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.AssetRegistry;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Pak;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;
using EpicManifestParser;
using EpicManifestParser.UE;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.CUE4Parse;
using FortnitePorting.Models.Fortnite;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Views;
using Serilog;
using UE4Config.Parsing;
using FGuid = CUE4Parse.UE4.Objects.Core.Misc.FGuid;

namespace FortnitePorting.Services;

public partial class CUE4ParseService : ObservableObject, IService
{
    [ObservableProperty] private string _status;
    [ObservableProperty] private bool _finishedLoading;

    public HybridFileProvider Provider;

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
    
    public static readonly DirectoryInfo CacheFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".cache"));

    public CUE4ParseService()
    {
        CacheFolder.Create();
    }

    public async Task Initialize()
    {
        Provider = AppSettings.Installation.CurrentProfile.FortniteVersion switch
        {
            EFortniteVersion.LatestOnDemand => new HybridFileProvider(new VersionContainer(AppSettings.Installation.CurrentProfile.UnrealVersion)),
            EFortniteVersion.LatestInstalled => new HybridFileProvider(AppSettings.Installation.CurrentProfile.ArchiveDirectory, ExtraDirectories, new VersionContainer(LATEST_GAME_VERSION)),
            _ => new HybridFileProvider(AppSettings.Installation.CurrentProfile.ArchiveDirectory, [], new VersionContainer(AppSettings.Installation.CurrentProfile.UnrealVersion)),
        };
        
		ObjectTypeRegistry.RegisterEngine(Assembly.Load("FortnitePorting"));
        ObjectTypeRegistry.RegisterEngine(Assembly.Load("FortnitePorting.Shared"));

        Provider.LoadExtraDirectories = AppSettings.Installation.CurrentProfile.LoadCreativeMaps;
        
        _onlineStatus = await Api.FortnitePorting.Online() ?? new OnlineResponse();
        
        await CheckBlackHole();
        await CleanupCache();

        Provider.VfsMounted += (sender, _) =>
        {
            if (sender is not IAesVfsReader reader) return;

            UpdateStatus($"Loading {reader.Name}");
        };
        
        UpdateStatus("Loading Native Libraries");
        await InitializeOodle();
        await InitializeZlib();
        await InitializeDetex();
        
        UpdateStatus("Loading Game Files");
        await InitializeProvider();
        await InitializeTextureStreaming();
        
        await LoadKeys();
        Provider.LoadVirtualPaths();
        Provider.PostMount();
        
        if (!Provider.TryChangeCulture(Provider.GetLanguageCode(AppSettings.Installation.CurrentProfile.GameLanguage)))
        {
            Info.Message("Internationalization", $"Failed to load language \"{AppSettings.Installation.CurrentProfile.GameLanguage.GetDescription()}\"");
        }
        
        await LoadMappings();
        
        await LoadAssetRegistries();

        UpdateStatus("Loading Application Assets");
        await LoadApplicationAssets();

        UpdateStatus(string.Empty);

        FinishedLoading = true;
    }

    private void UpdateStatus(string status)
    {
        Status = status;
    }

    private async Task CheckBlackHole()
    {
        if (AppSettings.Installation.CurrentProfile.FortniteVersion is not EFortniteVersion.LatestInstalled) return;
        
        var aes = _onlineStatus.Backup.Keys
            ? await Api.FortnitePorting.Aes()
            : await Api.FortniteCentral.Aes();
        if (aes is null) return;
        
        var mainPakPath = Path.Combine(AppSettings.Installation.CurrentProfile.ArchiveDirectory,
            "pakchunk0-WindowsClient.pak");
        if (!File.Exists(mainPakPath)) return;

        var mainPakReader = new PakFileReader(mainPakPath);
        if (mainPakReader.TestAesKey(new FAesKey(aes.MainKey))) return;
        
        BlackHole.Open(isMinigame: false);
    }
    
    private async Task CleanupCache()
    {
        var files = CacheFolder.GetFiles("*.*chunk");
        var cutoffDate = DateTime.Now - TimeSpan.FromDays(AppSettings.Debug.ChunkCacheLifetime);
        foreach (var file in files)
        {
            if (file.LastWriteTime >= cutoffDate) continue;
            
            file.Delete();
        }
    }
    
    private async Task InitializeOodle()
    {
        var oodlePath = Path.Combine(App.DataFolder.FullName, OodleHelper.OODLE_DLL_NAME);
        if (!File.Exists(oodlePath)) await OodleHelper.DownloadOodleDllAsync(oodlePath);
        OodleHelper.Initialize(oodlePath);
    }
    
    private async Task InitializeZlib()
    {
        var zlibPath = Path.Combine(App.DataFolder.FullName, ZlibHelper.DLL_NAME);
        if (!File.Exists(zlibPath)) await ZlibHelper.DownloadDllAsync(zlibPath);
        ZlibHelper.Initialize(zlibPath);
    }
    
    private async Task InitializeDetex()
    {
        TextureDecoder.UseAssetRipperTextureDecoder = true;
        var detexPath = Path.Combine(App.DataFolder.FullName, DetexHelper.DLL_NAME);
        if (!File.Exists(detexPath)) await DetexHelper.LoadDllAsync(detexPath);
        DetexHelper.Initialize(detexPath);
    }
    
    private async Task InitializeProvider()
    {
        if (AppSettings.Installation.CurrentProfile.FortniteVersion is EFortniteVersion.LatestInstalled or EFortniteVersion.LatestOnDemand)
        {
            await Api.EpicGames.VerifyAuthAsync();
        }
        
        switch (AppSettings.Installation.CurrentProfile.FortniteVersion)
        {
            case EFortniteVersion.LatestOnDemand:
            {
                var manifestInfo = await Api.EpicGames.GetManifestInfoAsync();
                if (manifestInfo is null) break;

                var options = new ManifestParseOptions
                {
                    ChunkBaseUrl = "http://download.epicgames.com/Builds/Fortnite/CloudDir/",
                    ChunkCacheDirectory = CacheFolder.FullName,
                    ManifestCacheDirectory = CacheFolder.FullName,
                    Decompressor = ManifestZlibStreamDecompressor.Decompress,
                    DecompressorState = ZlibHelper.Instance,
                    CacheChunksAsIs = true
                };
                
                var (manifest, element) = await manifestInfo.DownloadAndParseAsync(options);
                LiveManifest = manifest;

                UpdateStatus($"Loading Fortnite On-Demand (This may take a while)");

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
                break;
            }
        }
    }

    private async Task InitializeTextureStreaming()
    {
        if (AppSettings.Installation.CurrentProfile.FortniteVersion is not (EFortniteVersion.LatestInstalled or EFortniteVersion.LatestOnDemand)) return;
        if (!AppSettings.Installation.CurrentProfile.UseTextureStreaming) return;

        try
        {
            var tocPath = await GetTocPath(AppSettings.Installation.CurrentProfile.FortniteVersion);
            if (string.IsNullOrEmpty(tocPath)) return;

            var tocName = tocPath.SubstringAfterLast("/");
            var onDemandFile = new FileInfo(Path.Combine(App.DataFolder.FullName, tocName));
            if (!onDemandFile.Exists || onDemandFile.Length == 0)
            {
                await Api.DownloadFileAsync($"https://download.epicgames.com/{tocPath}", onDemandFile.FullName);
            }

            var options = new IoStoreOnDemandOptions
            {
                ChunkBaseUri = new Uri("https://download.epicgames.com/ias/fortnite/", UriKind.Absolute),
                ChunkCacheDirectory = CacheFolder,
                Authorization = new AuthenticationHeaderValue("Bearer", AppSettings.Application.EpicAuth?.Token),
                Timeout = TimeSpan.FromSeconds(AppSettings.Debug.RequestTimeoutSeconds)
            };

            var chunkToc = new IoChunkToc(onDemandFile);
            await Provider.RegisterVfs(chunkToc, options);
            await Provider.MountAsync();
        }
        catch (Exception e)
        {
            Info.Dialog("Failed to Initialize Texture Streaming", 
                $"Please enable the \"Pre-Download Streamed Assets\" option for Fortnite in the Epic Games Launcher and disable texture streaming in installation settings to remove this popup.");
        }
    }
    
    private async Task<string> GetTocPath(EFortniteVersion loadingType)
    {
        var onDemandText = string.Empty;
        switch (loadingType)
        {
            case EFortniteVersion.LatestInstalled:
            {
                var onDemandPath = Path.Combine(AppSettings.Installation.CurrentProfile.ArchiveDirectory, @"..\..\..\Cloud\IoStoreOnDemand.ini");
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
        switch (AppSettings.Installation.CurrentProfile.FortniteVersion)
        {
            case EFortniteVersion.LatestInstalled:
            case EFortniteVersion.LatestOnDemand:
            {
                var aes = _onlineStatus.Backup.Keys 
                    ? await Api.FortnitePorting.Aes() 
                    : await Api.FortniteCentral.Aes();
                
                if (aes is null)
                {
                    await LoadLocalKeys();
                    break;
                }

                AppSettings.Installation.CurrentProfile.MainKey = new FileEncryptionKey(aes.MainKey);
                await Provider.SubmitKeyAsync(Globals.ZERO_GUID, new FAesKey(aes.MainKey));
                
                AppSettings.Installation.CurrentProfile.ExtraKeys.Clear();
                foreach (var key in aes.DynamicKeys)
                {
                    AppSettings.Installation.CurrentProfile.ExtraKeys.Add(new FileEncryptionKey(key.Key));
                    await Provider.SubmitKeyAsync(new FGuid(key.GUID), new FAesKey(key.Key));
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
        var mainKey = AppSettings.Installation.CurrentProfile.MainKey;
        if (mainKey.IsEmpty) mainKey = FileEncryptionKey.Empty;
                
        await Provider.SubmitKeyAsync(Globals.ZERO_GUID, mainKey.EncryptionKey);
                
        foreach (var vfs in Provider.UnloadedVfs.ToArray())
        {
            foreach (var extraKey in AppSettings.Installation.CurrentProfile.ExtraKeys)
            {
                if (extraKey.IsEmpty) continue;
                if (!vfs.TestAesKey(extraKey.EncryptionKey)) continue;
                        
                await Provider.SubmitKeyAsync(vfs.EncryptionKeyGuid, extraKey.EncryptionKey);
            }
        }
    }
    
    private async Task LoadMappings()
    {
        var mappingsPath = AppSettings.Installation.CurrentProfile.FortniteVersion switch
        {
            EFortniteVersion.LatestInstalled or EFortniteVersion.LatestOnDemand => await GetEndpointMappings() ?? GetLocalMappings(),
            _ when AppSettings.Installation.CurrentProfile.UseMappingsFile && File.Exists(AppSettings.Installation.CurrentProfile.MappingsFile) => AppSettings.Installation.CurrentProfile.MappingsFile,
            _ => string.Empty
        };
        
        if (string.IsNullOrEmpty(mappingsPath)) return;
        
        Provider.MappingsContainer = new FileUsmapTypeMappingsProvider(mappingsPath);
        Log.Information("Loaded Mappings: {Path}", mappingsPath);
    }
    
    private async Task<string?> GetEndpointMappings()
    {
        async Task<string?> GetMappings(Func<Task<MappingsResponse[]?>> mappingsFunc)
        {
            var mappings = await mappingsFunc();
            if (mappings is null) return null;
            if (mappings.Length <= 0) return null;

            var foundMappings = mappings.FirstOrDefault();
            if (foundMappings is null) return null;

            var mappingsFilePath = Path.Combine(App.DataFolder.FullName, foundMappings.Filename);
            if (File.Exists(mappingsFilePath)) return mappingsFilePath;

            var createdFile = await Api.DownloadFileAsync(foundMappings.URL, mappingsFilePath);
            if (createdFile is null) return null;
            
            File.SetCreationTime(mappingsFilePath, foundMappings.Uploaded);

            return mappingsFilePath;
        }
        
        
        return  _onlineStatus.Backup.Mappings
        ? await GetMappings(Api.FortnitePorting.Mappings)
        : await GetMappings(Api.FortniteCentral.Mappings);
    }


    private string? GetLocalMappings()
    {
        var usmapFiles = App.DataFolder.GetFiles("*.usmap");
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

            UpdateStatus($"Loading {file.Name}");
            var assetArchive = await file.SafeCreateReaderAsync();
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
        if (await Provider.SafeLoadPackageObjectAsync("FortniteGame/Content/Balance/RarityData") is { } rarityData)
        {
            for (var i = 0; i < rarityData.Properties.Count; i++)
                RarityColors.Add(rarityData.GetByIndex<FRarityCollection>(i));
        }

        if (await Provider.SafeLoadPackageObjectAsync("/BeanstalkCosmetics/Cosmetics/DataTables/DT_BeanstalkCosmetics_Colors") is UDataTable beanstalkColorTable)
        {
            foreach (var (name, fallback) in beanstalkColorTable.RowMap)
            {
                var index = int.Parse(name.Text);
                BeanstalkColors[index] = fallback.GetOrDefault<FColor>("Color");
            }
        }
        
        if (await Provider.SafeLoadPackageObjectAsync("/BeanstalkCosmetics/Cosmetics/DataTables/DT_BeanstalkCosmetics_MaterialTypes") is UDataTable beanstalkMaterialTypesTable)
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
        
        if (await Provider.SafeLoadPackageObjectAsync("/BeanstalkCosmetics/Cosmetics/DataTables/DT_PatternAtlasTextureSlots") is UDataTable beanstalkAtlasSlotsTable)
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
            MaleLobbyMontages.AddIfNotNull(await Provider.SafeLoadPackageObjectAsync<UAnimMontage>(path));
        }
        
        foreach (var path in FemaleLobbyMontagePaths)
        {
            FemaleLobbyMontages.AddIfNotNull(await Provider.SafeLoadPackageObjectAsync<UAnimMontage>(path));
        }
    }
}