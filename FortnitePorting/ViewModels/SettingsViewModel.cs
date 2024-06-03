using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Versions;
using FortnitePorting.Models.Radio;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;
using NAudio.Wave;

namespace FortnitePorting.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    // ViewModels
    [ObservableProperty] private ExportSettingsViewModel _exportSettings = new();
    
    // Welcome
    [ObservableProperty] private bool _finishedWelcomeScreen;
    
    // todo save in presets class
    // Installation
    [ObservableProperty] private EFortniteVersion _fortniteVersion = EFortniteVersion.LatestInstalled;
    [ObservableProperty] private string _archiveDirectory;
    [ObservableProperty] private EGame _unrealVersion = EGame.GAME_UE5_LATEST;
    [ObservableProperty] private string _encryptionKey;
    [ObservableProperty] private bool _useMappingsFile;
    [ObservableProperty] private string _mappingsFile;
    [ObservableProperty] private ELanguage _gameLanguage = ELanguage.English;
    [ObservableProperty] private bool _useTextureStreaming = true;

    // Filtered Data
    [ObservableProperty] private HashSet<string> _filteredProps = [];

    // Radio
    [ObservableProperty] private RadioPlaylistSerializeData[] _playlists = [];
    [ObservableProperty] private int _audioDeviceIndex = 0;
    [ObservableProperty] private float _volume = 1.0f;
}