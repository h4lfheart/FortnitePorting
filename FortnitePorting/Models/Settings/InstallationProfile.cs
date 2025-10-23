using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Versions;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Models.CUE4Parse;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Validators;
using FortnitePorting.Validators;
using Newtonsoft.Json;

namespace FortnitePorting.Models.Settings;

public partial class InstallationProfile : ObservableValidator
{
    [ObservableProperty] private string _profileName = "Unnammed";
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ArchiveDirectoryEnabled))]
    [NotifyPropertyChangedFor(nameof(ArchiveDirectory))]
    [NotifyPropertyChangedFor(nameof(UnrealVersionEnabled))]
    [NotifyPropertyChangedFor(nameof(EncryptionKeyEnabled))]
    [NotifyPropertyChangedFor(nameof(MappingsFileEnabled))]
    [NotifyPropertyChangedFor(nameof(TextureStreamingEnabled))]
    [NotifyPropertyChangedFor(nameof(LoadCreativeMapsEnabled))]
    [NotifyPropertyChangedFor(nameof(IsCustom))]
    private EFortniteVersion _fortniteVersion = EFortniteVersion.LatestInstalled;
    
    [NotifyDataErrorInfo]
    [ArchiveDirectory(canValidateProperty: nameof(ArchiveDirectoryEnabled))]
    [ObservableProperty] private string _archiveDirectory;
    
    [ObservableProperty] private EGame _unrealVersion = EGame.GAME_UE5_7;
    
    [NotifyDataErrorInfo]
    [EncryptionKey(canValidateProperty: nameof(EncryptionKeyEnabled))]
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
    [ObservableProperty] private bool _loadCreativeMaps = true;

    [JsonIgnore] public bool IsCustom => FortniteVersion is EFortniteVersion.Custom;
    [JsonIgnore] public bool ArchiveDirectoryEnabled => FortniteVersion is not EFortniteVersion.LatestOnDemand;
    [JsonIgnore] public bool UnrealVersionEnabled => IsCustom;
    [JsonIgnore] public bool EncryptionKeyEnabled => IsCustom;
    [JsonIgnore] public bool MappingsFileEnabled => IsCustom;
    [JsonIgnore] public bool TextureStreamingEnabled => FortniteVersion is EFortniteVersion.LatestInstalled;
    [JsonIgnore] public bool LoadCreativeMapsEnabled => FortniteVersion is EFortniteVersion.LatestInstalled;
    
    public async Task BrowseArchivePath()
    {
        if (await BrowseFolderDialog() is { } path)
        {
            ArchiveDirectory = path;
        }
    }
    
    public async Task BrowseMappingsFile()
    {
        if (await BrowseFileDialog(fileTypes: Globals.MappingsFileType, suggestedFileName: MappingsFile) is { } path)
        {
            MappingsFile = path;
        }
    }

    public async Task FetchKeys()
    {
        var keys = await ApiVM.FortnitePorting.GetKeysAsync(FetchKeysVersion);
        if (keys is null)  
        {
            AppWM.Message("Fetch Keys", $"Unsuccessfully fetched keys for v{FetchKeysVersion}", InfoBarSeverity.Error);
            return;
        }

        MainKey = FileEncryptionKey.Empty;
        ExtraKeys.Clear();

        MainKey = new FileEncryptionKey(keys.MainKey);
        foreach (var dynamicKey in keys.DynamicKeys)
        {
            ExtraKeys.Add(new FileEncryptionKey(dynamicKey.Key));
        }
        
        AppWM.Message("Fetch Keys", $"Successfully fetched {keys.DynamicKeys.Count + 1} keys for v{FetchKeysVersion}", InfoBarSeverity.Success);
    }
    
    public async Task FetchMappings()
    {
        var mappings = await ApiVM.FortnitePorting.GetMappingsAsync(FetchMappingsVersion);
        if (mappings?.Url is null)
        {
            AppWM.Message("Fetch Mappings", $"Unsuccessfully fetched mappings for v{FetchMappingsVersion}", InfoBarSeverity.Error);
            return;
        }

        var mappingsFilePath = Path.Combine(DataFolder.FullName, mappings.Version + ".usmap");
        if (!File.Exists(mappingsFilePath))
        {
            var downloadedMappingsInfo = await ApiVM.DownloadFileAsync(mappings.Url, mappingsFilePath);
            if (!downloadedMappingsInfo.Exists)
            {
                AppWM.Message("Fetch Mappings", $"Unsuccessfully downloaded mappings for v{FetchMappingsVersion}", InfoBarSeverity.Error);
                return;
            }
        }
        
        MappingsFile = mappingsFilePath;
        UseMappingsFile = true;
        File.SetCreationTime(mappingsFilePath, mappings.GetCreationTime());
        
        AppWM.Message("Fetch Mappings", $"Successfully fetched mappings for v{FetchMappingsVersion}", InfoBarSeverity.Success);
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

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        switch (e.PropertyName)
        {
            case nameof(FortniteVersion):
            {
                ValidateAllProperties();
                break;
            }
        }
    }
}