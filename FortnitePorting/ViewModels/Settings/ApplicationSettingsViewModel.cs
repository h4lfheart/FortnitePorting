using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Media.Animation;
using FortnitePorting.Framework;
using FortnitePorting.Models;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Assets.Base;
using FortnitePorting.Models.Radio;
using FortnitePorting.Validators;
using NAudio.Wave;
using Newtonsoft.Json;

namespace FortnitePorting.ViewModels.Settings;

public partial class ApplicationSettingsViewModel : SettingsViewModelBase
{
    [property: RequiresRestart] [NotifyDataErrorInfo] [DirectoryExists("Application Data Path")] [ObservableProperty]
    private string _appDataPath;
    
    [property: RequiresRestart] [ObservableProperty] private bool _useAppDataPath;
    
    [property: RequiresRestart] [NotifyDataErrorInfo] [DirectoryExists("Assets Path")] [ObservableProperty]
    private string _assetsPath;
    
    [property: RequiresRestart] [ObservableProperty] private bool _useAssetsPath;
    
    [property: RequiresRestart] [ObservableProperty] private string _portleExecutablePath;
    [property: RequiresRestart] [ObservableProperty] private bool _usePortlePath;
    
    [ObservableProperty] private HashSet<string> _favoriteAssets = [];

    [ObservableProperty] private int _audioDeviceIndex = 0;
    [ObservableProperty] private RadioPlaylistSerializeData[] _playlists = [];
    [ObservableProperty] private float _volume = 1.0f;

    [ObservableProperty] private FPVersion _lastOnlineVersion = Globals.Version;


    [ObservableProperty] private bool _useTabTransitions = true;
    [ObservableProperty] private float _assetScale = 1.0f;
    
    [ObservableProperty] private bool _dontAskAboutKofi;
    [ObservableProperty] private DateTime _nextKofiAskDate = DateTime.Today;
    [ObservableProperty] private bool _showAssetNames;
        
    [ObservableProperty] private EpicAuthResponse? _epicAuth;
    
    public string AssetPath => UseAssetsPath && Directory.Exists(AssetsPath) ? AssetsPath : App.AssetsFolder.FullName;
    public string PortlePath => UsePortlePath && Directory.Exists(PortleExecutablePath) 
        ? PortleExecutablePath 
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Portle", "Portle.exe");


    [JsonIgnore]
    public NavigationTransitionInfo Transition => UseTabTransitions
        ? new SlideNavigationTransitionInfo()
        : new SuppressNavigationTransitionInfo();
    
    
    public DirectSoundDeviceInfo[] AudioDevices => DirectSoundOut.Devices.ToArray()[1..];

    public async Task BrowseAppDataPath()
    {
        if (await App.BrowseFolderDialog() is { } path) AppDataPath = path;
        
    }
    public async Task BrowseAssetsPath()
    {
        if (await App.BrowseFolderDialog() is { } path) AssetsPath = path;
    }
    
    public async Task BrowsePortlePath()
    {
        if (await App.BrowseFileDialog() is { } path) PortleExecutablePath = path;
    }

    protected override async void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        switch (e.PropertyName)
        {
            case nameof(AudioDeviceIndex):
            {
                MusicVM?.UpdateOutputDevice();
                SoundPreviewWM?.UpdateOutputDevice();
                break;
            }
            case nameof(AssetScale):
            {
                BaseAssetItem.SetScale(AssetScale);
                break;
            }
        }
    }
}