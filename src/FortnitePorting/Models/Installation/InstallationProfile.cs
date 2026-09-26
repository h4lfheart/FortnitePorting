using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Models.CUE4Parse;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Validators;
using Newtonsoft.Json;

namespace FortnitePorting.Models.Installation;

public partial class InstallationProfile : ObservableValidator
{
    [ObservableProperty] private string _profileName = "Unnammed";
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ArchiveDirectoryEnabled))]
    [NotifyPropertyChangedFor(nameof(UnrealVersionEnabled))]
    [NotifyPropertyChangedFor(nameof(EncryptionKeyEnabled))]
    [NotifyPropertyChangedFor(nameof(MappingsFileEnabled))]
    [NotifyPropertyChangedFor(nameof(TextureStreamingEnabled))]
    [NotifyPropertyChangedFor(nameof(LoadInstalledBundlesEnabled))]
    [NotifyPropertyChangedFor(nameof(IsCustom))]
    private EFortniteVersion _fortniteVersion = EFortniteVersion.LatestInstalled;
    
    [NotifyDataErrorInfo]
    [ArchiveDirectory(canValidateProperty: nameof(ArchiveDirectoryEnabled))]
    [ObservableProperty] private string _archiveDirectory = string.Empty;
    
    [ObservableProperty] private EGame _unrealVersion = EGame.GAME_UE6_0;
    
    [NotifyDataErrorInfo]
    [EncryptionKey(canValidateProperty: nameof(EncryptionKeyEnabled))]
    [ObservableProperty] 
    private FileEncryptionKey _mainKey = FileEncryptionKey.Empty;
    
    [ObservableProperty] private ObservableCollection<FileEncryptionKey> _extraKeys = [];

    [NotifyPropertyChangedFor(nameof(CanFetchVersion))]
    [ObservableProperty] [property: JsonIgnore]
    private string _fetchVersion = string.Empty;
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(MappingsFileEnabled))]
    private bool _useMappingsFile;
    
    [ObservableProperty] private string _mappingsFile = string.Empty;
    
    [ObservableProperty] private ELanguage _gameLanguage = ELanguage.English;
    [ObservableProperty] private bool _useTextureStreaming = true;
    [ObservableProperty] private bool _loadInstalledBundles = true;
    [ObservableProperty] private bool _loadNaniteData = true;

    [ObservableProperty] private bool _isSelected;

    [JsonIgnore] public bool IsCustom => FortniteVersion is EFortniteVersion.Custom;
    [JsonIgnore] public bool ArchiveDirectoryEnabled => FortniteVersion is not EFortniteVersion.LatestOnDemand;
    [JsonIgnore] public bool UnrealVersionEnabled => IsCustom;
    [JsonIgnore] public bool EncryptionKeyEnabled => IsCustom;
    [JsonIgnore] public bool MappingsFileEnabled => IsCustom;
    [JsonIgnore] public bool TextureStreamingEnabled => FortniteVersion is EFortniteVersion.LatestInstalled;
    [JsonIgnore] public bool LoadInstalledBundlesEnabled => FortniteVersion is EFortniteVersion.LatestInstalled;
    [JsonIgnore] public bool CanFetchVersion => !string.IsNullOrWhiteSpace(FetchVersion);
    
    public async Task BrowseArchivePath()
    {
        if (await App.BrowseFolderDialog() is { } path)
        {
            ArchiveDirectory = path;
        }
    }
    
    public async Task BrowseMappingsFile()
    {
        if (await App.BrowseFileDialog(fileTypes: Globals.MappingsFileType, suggestedFileName: MappingsFile) is { } path)
        {
            MappingsFile = path;
        }
    }

    public async Task FetchVersionData()
    {
        if (!CanFetchVersion) return;

        var version = FetchVersion.Trim();
        var response = await Api.FortnitePorting.FortniteVersion(version);
        if (response is null)
        {
            Info.Message("Fetch Data", $"Failed to data for {version}", InfoBarSeverity.Error);
            return;
        }

        MainKey = new FileEncryptionKey(response.Keys.MainKey.Key);
        ExtraKeys.Clear();
        foreach (var extraKey in response.Keys.ExtraKeys)
        {
            ExtraKeys.Add(new FileEncryptionKey(extraKey.Key));
        }

        var mappingsFound = false;
        if (response.Mappings?.Url is not null)
        {
            var mappingsFilePath = Path.Combine(App.DataFolder.FullName, response.Mappings.Url.SubstringAfterLast("/"));
            if (!File.Exists(mappingsFilePath) ||
                !new FileInfo(mappingsFilePath).GetFileHashMD5().Equals(response.Mappings.Md5Hash))
            {
                var downloaded = await Api.DownloadFileAsync(response.Mappings.Url, mappingsFilePath);
                if (downloaded is not { Exists: true })
                {
                    Info.Message("Fetch Data", $"Failed to download mappings for {version}", InfoBarSeverity.Error);
                    return;
                }

                File.SetCreationTime(mappingsFilePath, DateTime.Now);
            }

            MappingsFile = mappingsFilePath;
            UseMappingsFile = true;
            mappingsFound = true;
        }
        else
        {
            UseMappingsFile = false;
            MappingsFile = string.Empty;
        }

        var keyCount = response.Keys.ExtraKeys.Count + 1;
        var mappingsMessage = mappingsFound
            ? "and downloaded mappings for this version."
            : "but mappings were not available for this version";
        
        Info.Message("Fetch Data", $"Successfully fetched {keyCount} keys for {response.Version} {mappingsMessage}",
            InfoBarSeverity.Success);
    }
    
    public async Task AddEncryptionKey()
    {
        ExtraKeys.Add(FileEncryptionKey.Empty);
    }
    
    public async Task RemoveEncryptionKey(FileEncryptionKey? key)
    {
        if (key is null) return;
        ExtraKeys.Remove(key);
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
