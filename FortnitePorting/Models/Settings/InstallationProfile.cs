using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Versions;
using FortnitePorting.Application;
using FortnitePorting.Models.CUE4Parse;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Validators;
using Newtonsoft.Json;

namespace FortnitePorting.Models.Settings;

public partial class InstallationProfile : ObservableValidator
{
    [ObservableProperty] private string _profileName = "Unnammed";
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ArchiveDirectoryEnabled))]
    [NotifyPropertyChangedFor(nameof(UnrealVersionEnabled))]
    [NotifyPropertyChangedFor(nameof(EncryptionKeyEnabled))]
    [NotifyPropertyChangedFor(nameof(MappingsFileEnabled))]
    [NotifyPropertyChangedFor(nameof(TextureStreamingEnabled))]
    [NotifyPropertyChangedFor(nameof(IsCustom))]
    private EFortniteVersion _fortniteVersion = EFortniteVersion.LatestInstalled;
    
    [NotifyDataErrorInfo]
    [ArchiveDirectory]
    [ObservableProperty] private string _archiveDirectory;
    
    [ObservableProperty] private EGame _unrealVersion = EGame.GAME_UE5_LATEST;
    
    [NotifyDataErrorInfo]
    [EncryptionKey]
    [ObservableProperty] 
    private FileEncryptionKey _mainKey = FileEncryptionKey.Empty;
    
    [ObservableProperty] private int _selectedExtraKeyIndex;
    [ObservableProperty] private ObservableCollection<FileEncryptionKey> _extraKeys = [];
    [ObservableProperty, JsonIgnore] private string _fetchKeysVersion = string.Empty;
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(MappingsFileEnabled))]
    private bool _useMappingsFile;
    
    [ObservableProperty] private string _mappingsFile = string.Empty;
    [ObservableProperty, JsonIgnore] private string _fetchMappingsVersion = string.Empty;
    
    [ObservableProperty] private ELanguage _gameLanguage = ELanguage.English;
    [ObservableProperty] private bool _useTextureStreaming = true;

    [JsonIgnore] public bool IsCustom => FortniteVersion is EFortniteVersion.Custom;
    [JsonIgnore] public bool ArchiveDirectoryEnabled => FortniteVersion is not EFortniteVersion.LatestOnDemand;
    [JsonIgnore] public bool UnrealVersionEnabled => IsCustom;
    [JsonIgnore] public bool EncryptionKeyEnabled => IsCustom;
    [JsonIgnore] public bool MappingsFileEnabled => IsCustom;
    [JsonIgnore] public bool TextureStreamingEnabled => FortniteVersion is EFortniteVersion.LatestInstalled;
    
    public async Task BrowseArchivePath()
    {
        if (await BrowseFolderDialog() is { } path)
        {
            ArchiveDirectory = path;
        }
    }
    
    public async Task BrowseMappingsFile()
    {
        if (await BrowseFileDialog(fileTypes: Globals.MappingsFileType) is { } path)
        {
            MappingsFile = path;
        }
    }

    public async Task FetchKeys()
    {
        var keys = await ApiVM.FortniteCentral.GetKeysAsync(FetchKeysVersion);
        if (keys is null) return;

        MainKey = FileEncryptionKey.Empty;
        ExtraKeys.Clear();

        MainKey = new FileEncryptionKey(keys.MainKey);
        foreach (var dynamicKey in keys.DynamicKeys)
        {
            ExtraKeys.Add(new FileEncryptionKey(dynamicKey.Key));
        }
    }
    
    public async Task FetchMappings()
    {
        var mappings = await ApiVM.FortniteCentral.GetMappingsAsync(FetchKeysVersion);
        var targetMappings = mappings?.FirstOrDefault();
        if (targetMappings is null) return;

        var mappingsFilePath = Path.Combine(DataFolder.FullName, targetMappings.Filename);
        if (File.Exists(mappingsFilePath)) return;

        await ApiVM.DownloadFileAsync(targetMappings.URL, mappingsFilePath);
        MappingsFile = mappingsFilePath;
    }
    
    public async Task AddEncryptionKey()
    {
        ExtraKeys.Add(FileEncryptionKey.Empty);
    }
    
    public async Task RemoveEncryptionKey()
    {
        var selectedIndexToRemove = SelectedExtraKeyIndex;
        ExtraKeys.RemoveAt(selectedIndexToRemove);
        SelectedExtraKeyIndex = selectedIndexToRemove == 0 ? 0 : selectedIndexToRemove - 1;
    }

    public override string ToString()
    {
        return ProfileName;
    }
}