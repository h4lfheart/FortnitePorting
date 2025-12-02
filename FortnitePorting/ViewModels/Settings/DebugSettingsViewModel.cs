using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;

namespace FortnitePorting.ViewModels.Settings;

public partial class DebugSettingsViewModel : SettingsViewModelBase
{
     [ObservableProperty] private int _chunkCacheLifetime = 1;
     [ObservableProperty] private int _requestTimeoutSeconds = 10;
    [ObservableProperty] private bool _showMapDebugInfo = false;
    [ObservableProperty] private bool _isConsoleVisible = false;
}