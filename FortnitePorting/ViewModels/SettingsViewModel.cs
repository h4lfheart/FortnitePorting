using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Versions;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;

namespace FortnitePorting.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    
    [ObservableProperty] private bool _finishedWelcomeScreen;
    
    // todo save in presets class
    [ObservableProperty] private EFortniteVersion _fortniteVersion = EFortniteVersion.LatestInstalled;
    [ObservableProperty] private string _archiveDirectory;
    [ObservableProperty] private EGame _unrealVersion = EGame.GAME_UE5_LATEST;
    [ObservableProperty] private string _encryptionKey;
    [ObservableProperty] private bool _useMappingsFile;
    [ObservableProperty] private string _mappingsFile;
    [ObservableProperty] private ELanguage _gameLanguage = ELanguage.English;
    [ObservableProperty] private bool _useTextureStreaming = true;

    [ObservableProperty] private HashSet<string> _filteredProps = [];
}