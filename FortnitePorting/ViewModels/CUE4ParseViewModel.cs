using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.AssetRegistry;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using FortnitePorting.AppUtils;
using FortnitePorting.Services;
using FortnitePorting.Services.Endpoints.Models;

namespace FortnitePorting.ViewModels;

public class CUE4ParseViewModel : ObservableObject
{
    public readonly DefaultFileProvider Provider;

    public FAssetRegistryState? AssetRegistry;

    public CUE4ParseViewModel(string directory)
    {
        Provider = new DefaultFileProvider(directory, SearchOption.TopDirectoryOnly, isCaseInsensitive: true, new VersionContainer(EGame.GAME_UE5_LATEST));
    }
    
    public async Task Initialize()
    {
        Provider.Initialize();

        await InitializeKeys();
        await InitializeMappings();
        if (Provider.MappingsContainer is null)
        {
            AppLog.Warning("Failed to load mappings, issues may occur");
        }
        
        Provider.LoadLocalization(AppSettings.Current.Language);
        Provider.LoadVirtualPaths();
        
        var assetArchive = await Provider.TryCreateReaderAsync("FortniteGame/AssetRegistry.bin");
        if (assetArchive is not null)
        {
            AssetRegistry = new FAssetRegistryState(assetArchive);
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
}