using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.AssetRegistry;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using FortnitePorting.Application;
using FortnitePorting.Models.CUE4Parse;
using FortnitePorting.Models.Fortnite;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.Windows;
using Serilog;
using UE4Config.Parsing;

namespace FortnitePorting.ViewModels;

public class CUE4ParseViewModel : ViewModelBase
{
    public readonly HybridFileProvider Provider = AppSettings.Current.FortniteVersion switch
    {
        EFortniteVersion.LatestOnDemand => new HybridFileProvider(new VersionContainer(AppSettings.Current.UnrealVersion)),
        _ => new HybridFileProvider(AppSettings.Current.ArchiveDirectory, [], new VersionContainer(AppSettings.Current.UnrealVersion)),
    };
    
    public readonly List<FAssetData> AssetRegistry = [];
    public readonly List<FRarityCollection> RarityColors = [];
    

    public override async Task Initialize()
    {
        ObjectTypeRegistry.RegisterEngine(Assembly.GetAssembly(typeof(UCustomObject))!);
        
        HomeVM.UpdateStatus("Loading Native Libraries");
        await InitializeOodle();
        await InitializeZlib();
        
        HomeVM.UpdateStatus("Loading Game Files");
        await InitializeProvider();
        await InitializeTextureStreaming();
        
        await LoadKeys();
        Provider.LoadLocalization(AppSettings.Current.GameLanguage);
        Provider.LoadVirtualPaths();
        await LoadMappings();

        Provider.LoadIniConfigs();
        await LoadConsoleVariables();

        var mesh = Provider.LoadObject<USkeletalMesh>("FortniteGame/Content/Characters/Player/Male/Medium/Bodies/M_Med_Soldier_04/Meshes/SK_M_Med_Soldier_04");
        ModelPreviewWindow.Preview(mesh);
        
        HomeVM.UpdateStatus("Loading Asset Registry");
        await LoadAssetRegistries();

        HomeVM.UpdateStatus("Loading Application Assets");
        await LoadApplicationAssets();

        HomeVM.UpdateStatus(string.Empty);
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
        switch (AppSettings.Current.FortniteVersion)
        {
            case EFortniteVersion.LatestOnDemand:
            {
                // TODO Fortnite Live Support
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
        
    }

    private async Task LoadKeys()
    {
        switch (AppSettings.Current.FortniteVersion)
        {
            case EFortniteVersion.LatestInstalled:
            case EFortniteVersion.LatestOnDemand:
            {
                var aes = await ApiVM.FortniteCentral.GetKeysAsync();
                if (aes is null) return;
                
                await Provider.SubmitKeyAsync(Globals.ZERO_GUID, new FAesKey(aes.MainKey));
                foreach (var key in aes.DynamicKeys)
                {
                    await Provider.SubmitKeyAsync(new FGuid(key.GUID), new FAesKey(key.Key));
                }
                
                break;
            }
            default:
            {
                await Provider.SubmitKeyAsync(Globals.ZERO_GUID, new FAesKey(AppSettings.Current.EncryptionKey));
                // TODO Multiple Keys
                break;
            }
        }
    }
    
    private async Task LoadMappings()
    {
        var mappingsPath = AppSettings.Current.FortniteVersion switch
        {
            EFortniteVersion.LatestInstalled or EFortniteVersion.LatestOnDemand => await GetEndpointMappings() ?? GetLocalMappings(),
            _ when AppSettings.Current.UseMappingsFile && File.Exists(AppSettings.Current.MappingsFile) => AppSettings.Current.MappingsFile,
            _ => string.Empty
        };
        
        if (string.IsNullOrEmpty(mappingsPath)) return;
        
        Provider.MappingsContainer = new FileUsmapTypeMappingsProvider(mappingsPath);
        Log.Information("Loaded Mappings: {Path}", mappingsPath);
    }
    
    private async Task<string?> GetEndpointMappings()
    {
        var mappings =  await ApiVM.FortniteCentral.GetMappingsAsync();
        if (mappings is null) return null;
        if (mappings.Length <= 0) return null;

        var foundMappings = mappings.FirstOrDefault();
        if (foundMappings is null) return null;

        var mappingsFilePath = Path.Combine(DataFolder.FullName, foundMappings.Filename);
        if (File.Exists(mappingsFilePath)) return null;

        await ApiVM.DownloadFileAsync(foundMappings.URL, mappingsFilePath);
        return mappingsFilePath;
    }

    private string? GetLocalMappings()
    {
        var usmapFiles = DataFolder.GetFiles("*.usmap");
        if (usmapFiles.Length <= 0) return null;

        var latestUsmap = usmapFiles.MaxBy(x => x.LastWriteTime);
        return latestUsmap?.FullName;
    }
    
    private async Task LoadConsoleVariables()
    {
        var tokens = Provider.DefaultEngine.Sections.FirstOrDefault(source => source.Name == "ConsoleVariables")?.Tokens ?? [];
        foreach (var token in tokens)
        {
            if (token is not InstructionToken instructionToken) continue;
            var value = instructionToken.Value.Equals("1");
            
            switch (instructionToken.Key)
            {
                case "r.StaticMesh.KeepMobileMinLODSettingOnDesktop":
                case "r.SkeletalMesh.KeepMobileMinLODSettingOnDesktop":
                    Provider.Versions[instructionToken.Key[2..]] = value;
                    continue;
            }
        }
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
    }
}