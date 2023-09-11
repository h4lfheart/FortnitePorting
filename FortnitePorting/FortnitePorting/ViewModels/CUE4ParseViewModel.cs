using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.AssetRegistry;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using Serilog;

namespace FortnitePorting.ViewModels;

public class CUE4ParseViewModel : ViewModelBase
{
    public readonly HybridFileProvider Provider;
    public readonly List<FAssetData> AssetRegistry = new();
    public readonly RarityCollection[] RarityColors = new RarityCollection[8];
    public readonly HashSet<string> MeshEntries = new();

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
    
    public CUE4ParseViewModel()
    {
        Provider = AppSettings.Current.LoadingType switch
        {
            ELoadingType.Local => new HybridFileProvider(AppSettings.Current.LocalArchivePath, LatestVersionContainer),
            ELoadingType.Live => new HybridFileProvider(LatestVersionContainer),
            ELoadingType.Custom => new HybridFileProvider(AppSettings.Current.CustomArchivePath, new VersionContainer(AppSettings.Current.CustomUnrealVersion))
        };
    }

    public override async Task Initialize()
    {
        LoadingVM.LoadingTiers = 6;
        
        LoadingVM.Update("Loading Game Archive");
        await InitializeProvider();
        await InitializeKeys();
        
        Provider.LoadLocalization(AppSettings.Current.Language);
        Provider.LoadVirtualPaths();
        Provider.LoadVirtualCache();
        
        LoadingVM.Update("Loading Mappings");
        await InitializeMappings();
        
        LoadingVM.Update("Loading Content Builds");
        // TODO Content Builds Loading
        
        LoadingVM.Update("Loading Asset Registry");
        await LoadAssetRegistries();
        
        LoadingVM.Update("Loading Assets");
        await LoadRequiredAssets();
        
        LoadingVM.Update("Preloading Mesh Entries");
        await LoadMeshEntries();
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
            case ELoadingType.Live: // TODO Live Initialization
            {
                break;
            }
        }
    }
    
    private async Task InitializeKeys()
    {
        switch (AppSettings.Current.LoadingType)
        {
            case ELoadingType.Local:
            case ELoadingType.Live:
            {
                var aes = await EndpointService.FortniteCentral.GetKeysAsync();
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
    
    private async Task InitializeMappings()
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
        var mappings = await EndpointService.FortniteCentral.GetMappingsAsync();
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
            if (removeEntries.Contains(entry.Key)) continue;
            if (!entry.Key.EndsWith(".uasset") || entry.Key.EndsWith(".o.uasset")) continue;
            var entry1 = entry;
            if (MeshFilter.Any(x => entry1.Key.Contains(x, StringComparison.OrdinalIgnoreCase))) continue;

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