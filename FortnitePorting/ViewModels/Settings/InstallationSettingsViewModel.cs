using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Versions;
using FortnitePorting.Models.CUE4Parse;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Validators;
using Newtonsoft.Json;

namespace FortnitePorting.ViewModels.Settings;

public partial class InstallationSettingsViewModel : ViewModelBase
{
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
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(MappingsFileEnabled))]
    private bool _useMappingsFile;
    
    [ObservableProperty] private string _mappingsFile = string.Empty;
    
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
}