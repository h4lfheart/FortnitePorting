using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.FileProvider.Vfs;
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
using CUE4Parse.Utils;
using EpicManifestParser.Objects;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.Services.Endpoints;
using FortnitePorting.Services.Endpoints.Models;
using Serilog;

namespace FortnitePorting.ViewModels;

public class CUE4ParseViewModel : ViewModelBase
{
    public readonly HybridFileProvider Provider = AppSettings.Current.LoadingType switch
    {
        ELoadingType.Local => new HybridFileProvider(AppSettings.Current.LocalArchivePath, LatestVersionContainer),
        ELoadingType.Live => new HybridFileProvider(LatestVersionContainer),
        ELoadingType.Custom => new HybridFileProvider(AppSettings.Current.CustomArchivePath, new VersionContainer(AppSettings.Current.CustomUnrealVersion))
    };

    public Manifest? FortniteLive;
    public readonly List<FAssetData> AssetRegistry = new();
    public readonly RarityCollection[] RarityColors = new RarityCollection[8];
    public readonly HashSet<string> MeshEntries = new();
    //private BackupAPIResponse? BackupAPI;

    private static readonly Regex FortniteLiveRegex = new(@"^FortniteGame(/|\\)Content(/|\\)Paks(/|\\)(pakchunk(?:0|10.*|\w+)-WindowsClient|global)\.(pak|utoc)$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly VersionContainer LatestVersionContainer = new(EGame.GAME_UE5_3, optionOverrides: new Dictionary<string, bool>
    {
        { "SkeletalMesh.KeepMobileMinLODSettingOnDesktop", true },
        { "StaticMesh.KeepMobileMinLODSettingOnDesktop", true }
    });
    
    private static readonly string[] MeshFilter =
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

    public override async Task Initialize()
    {
        //BackupAPI = await EndpointService.FortnitePorting.GetBackupAPIAsync();

        HomeVM.Update("Loading Game Archive");
        await InitializeProvider();
        await LoadKeys();
        
        Provider.LoadLocalization(AppSettings.Current.Language);
        Provider.LoadVirtualPaths();
        Provider.LoadVirtualCache();
        await LoadMappings();
        
        HomeVM.Update("Loading Content Builds");
        if (AppSettings.Current.LoadContentBuilds)
        {
            await LoadContentBuilds();
        }
        
        HomeVM.Update("Loading Asset Registry");
        await LoadAssetRegistries();
        
        HomeVM.Update("Loading Assets");
        await LoadRequiredAssets();
        
        HomeVM.Update("Loading Mesh Entries");
        await LoadMeshEntries();
        
        HomeVM.Update(string.Empty);
    }
    
    private async Task InitializeProvider()
    {
        switch (AppSettings.Current.LoadingType)
        {
            case ELoadingType.Local:
            case ELoadingType.Custom:
            {
                Provider.InitializeLocal();
                break;
            }
            case ELoadingType.Live:
            {
                await InitializeFortniteLive();
                break;
            }
        }
    }
    
    public async Task InitializeFortniteLive()
    {
        if (FortniteLive is not null) return;

        var manifestInfo = await EndpointService.EpicGames.GetManifestInfoAsync();
        if (manifestInfo is null) return;

        HomeVM.Update($"Loading Fortnite Live v{manifestInfo.Version}");
        
        var manifestPath = Path.Combine(App.DataFolder.FullName, manifestInfo.FileName);
        FortniteLive = await EndpointService.EpicGames.GetManifestAsync(manifestInfo.Uri.AbsoluteUri, manifestPath);
        
        var files = FortniteLive.FileManifests.Where(fileManifest => FortniteLiveRegex.IsMatch(fileManifest.Name));
        foreach (var fileManifest in files)
        {
            Provider.Initialize(fileManifest.Name, new Stream[] { fileManifest.GetStream() }, it => new FStreamArchive(it, FortniteLive.FileManifests.First(x => x.Name.Equals(it)).GetStream(), Provider.Versions));
        }
    }
    
    private async Task LoadKeys()
    {
        switch (AppSettings.Current.LoadingType)
        {
            case ELoadingType.Local:
            case ELoadingType.Live:
            {
                var aes = await EndpointService.FortniteCentral.GetKeysAsync();// ?? BackupAPI?.GetKeys();
                if (aes is null) return;

                await Provider.SubmitKeyAsync(Globals.ZERO_GUID, new FAesKey(aes.MainKey));
                foreach (var key in aes.DynamicKeys)
                {
                    await Provider.SubmitKeyAsync(new FGuid(key.GUID), new FAesKey(key.Key));
                }
                break;
            }
            case ELoadingType.Custom:
            {
                await Provider.SubmitKeyAsync(Globals.ZERO_GUID, new FAesKey(AppSettings.Current.CustomEncryptionKey));
                // TODO Extra Custom Keys
                break;
            }
        }
    }
    
    private async Task LoadMappings()
    {
        switch (AppSettings.Current.LoadingType)
        {
            case ELoadingType.Local:
            case ELoadingType.Live:
            {
                var mappingsPath = await GetEndpointMappings() ?? GetLocalMappings();
                if (mappingsPath is null) return;
                
                Provider.MappingsContainer = new FileUsmapTypeMappingsProvider(mappingsPath);
                Log.Information("Loaded Mappings: {Path}", mappingsPath);
                break;
            }
            case ELoadingType.Custom:
            {
                if (!File.Exists(AppSettings.Current.CustomMappingsPath)) return; // optional
                Provider.MappingsContainer = new FileUsmapTypeMappingsProvider(AppSettings.Current.CustomMappingsPath);
                Log.Information("Loaded Mappings: {Path}", AppSettings.Current.CustomMappingsPath);
                break;
            }
        }
    }
    
    private async Task<string?> GetEndpointMappings()
    {
        var mappings = await EndpointService.FortniteCentral.GetMappingsAsync();// ?? BackupAPI?.GetMappings();
        if (mappings is null) return null;
        if (mappings.Length <= 0) return null;

        var foundMappings = mappings.FirstOrDefault(x => x.Meta.CompressionMethod.Equals("Oodle", StringComparison.OrdinalIgnoreCase));
        if (foundMappings is null) return null;

        var mappingsFilePath = Path.Combine(App.DataFolder.FullName, foundMappings.Filename);
        if (File.Exists(mappingsFilePath)) return null;

        await EndpointService.DownloadFileAsync(foundMappings.URL, mappingsFilePath);
        return mappingsFilePath;
    }
    
    private string? GetLocalMappings()
    {
        var usmapFiles = App.DataFolder.GetFiles("*.usmap");
        if (usmapFiles.Length <= 0) return null;

        var latestUsmap = usmapFiles.MaxBy(x => x.LastWriteTime);
        return latestUsmap?.FullName;
    }

    private async Task LoadContentBuilds()
    {
        if (AppSettings.Current.LoadingType == ELoadingType.Custom) return;

        var contentBuilds = await GetContentBuildsAsync(ELoadingType.Local);
        contentBuilds ??= await GetContentBuildsAsync(ELoadingType.Live); // fallback for invalid local buildinfo
        if (contentBuilds is null) return;

        var manifestInfo = contentBuilds.Items!.Manifest;
        var manifestUrl = manifestInfo.Distribution + manifestInfo.Path;
        var manifestPath = Path.Combine(App.DataFolder.FullName, manifestInfo.Path.SubstringAfterLast("/"));
        
        var manifest = await EndpointService.EpicGames.GetManifestAsync(manifestUrl, manifestPath);
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
    }
    
    async Task<ContentBuildsResponse?> GetContentBuildsAsync(ELoadingType type)
    {
        var buildInfoText = string.Empty;
        switch (type)
        {
            case ELoadingType.Local:
                var buildInfoPath = Path.Combine(AppSettings.Current.LocalArchivePath, "..\\..\\..\\Cloud\\BuildInfo.ini");
                buildInfoText = File.Exists(buildInfoPath) ? await File.ReadAllTextAsync(buildInfoPath) : string.Empty;
                break;
            case ELoadingType.Live:
                await InitializeFortniteLive(); // if used for fallback mode
                var buildInfoFile = FortniteLive?.FileManifests.FirstOrDefault(x => x.Name.Equals("Cloud/BuildInfo.ini", StringComparison.OrdinalIgnoreCase));
                if (buildInfoFile is null) return null;

                var stream = buildInfoFile.GetStream();
                var bytes = stream.ToBytes();
                buildInfoText = Encoding.UTF8.GetString(bytes);
                break;
        }

        if (string.IsNullOrEmpty(buildInfoText)) return null;

        var buildInfo = new SimpleIni(buildInfoText);
        var contentBuilds = await EndpointService.EpicGames.GetContentBuildsAsync(buildInfo["Content"]["Label"]);
        return contentBuilds?.Items is null ? null : contentBuilds;
    }
    
    private async Task LoadAssetRegistries()
    {
        var assetRegistries = Provider.Files.Where(x => x.Key.Contains("AssetRegistry", StringComparison.OrdinalIgnoreCase));
        foreach (var (path, file) in assetRegistries)
        {
            if (path.Contains("UEFN", StringComparison.OrdinalIgnoreCase) || path.Contains("Editor", StringComparison.OrdinalIgnoreCase)) continue;
            
            var assetArchive = await file.TryCreateReaderAsync();
            if (assetArchive is null) continue;

            try
            {
                var assetRegistry = new FAssetRegistryState(assetArchive);
                AssetRegistry.AddRange(assetRegistry.PreallocatedAssetDataBuffers);
                Log.Information("Loaded Asset Registry: {FilePath}", file.Path);
            }
            catch (Exception)
            {
                Log.Warning("Failed to load asset registry: {FilePath}", file.Path);
            }
        }
       
    }
    
    private async Task LoadRequiredAssets()
    {
        if (await Provider.TryLoadObjectAsync("FortniteGame/Content/Balance/RarityData") is { } rarityData)
        {
            for (var i = 0; i < 8; i++)
            {
                RarityColors[i] = rarityData.GetByIndex<RarityCollection>(i);
            }
        }
    }
    
    private async Task LoadMeshEntries()
    {
        var entries = Provider.Files.ToArray();
        var removeEntries = AssetRegistry.Select(x => Provider.FixPath(x.ObjectPath) + ".uasset").ToHashSet();

        foreach (var entry in entries)
        {
            if (!entry.Key.EndsWith(".uasset") || entry.Key.EndsWith(".o.uasset")) continue;
            if (removeEntries.Contains(entry.Key)) continue;
            if (MeshFilter.Any(x => entry.Key.Contains(x, StringComparison.OrdinalIgnoreCase))) continue;

            MeshEntries.Add(entry.Value.Path);
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