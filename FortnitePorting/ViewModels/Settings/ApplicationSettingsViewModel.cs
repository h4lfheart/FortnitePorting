using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Media.Animation;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Radio;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Models;
using FortnitePorting.Shared.Validators;
using NAudio.Wave;
using Newtonsoft.Json;
using Serilog;

namespace FortnitePorting.ViewModels.Settings;

public partial class ApplicationSettingsViewModel : ViewModelBase
{
    [NotifyDataErrorInfo] [DirectoryExists("Assets Path")] [ObservableProperty]
    private string _assetsPath;
    
    [ObservableProperty] private string _portleExecutablePath;
    
    [ObservableProperty] private HashSet<string> _favoriteAssets = [];

    [ObservableProperty] private int _audioDeviceIndex = 0;
    [ObservableProperty] private RadioPlaylistSerializeData[] _playlists = [];
    [ObservableProperty] private float _volume = 1.0f;

    [ObservableProperty] private FPVersion _lastOnlineVersion = Globals.Version;

    [ObservableProperty] private bool _useAssetsPath;
    [ObservableProperty] private bool _usePortlePath;

    [ObservableProperty] private bool _useTabTransitions = true;
    
    [ObservableProperty] private bool _dontAskAboutKofi;
    [ObservableProperty] private DateTime _nextKofiAskDate = DateTime.Today;
        
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
                RadioVM?.UpdateOutputDevice();
                SoundPreviewWM?.UpdateOutputDevice();
                break;
            }
        }
    }
}