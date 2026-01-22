using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Media.Animation;
using FortnitePorting.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Models;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Assets.Base;
using FortnitePorting.Models.Radio;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Validators;
using NAudio.Wave;
using Newtonsoft.Json;

namespace FortnitePorting.ViewModels.Settings;

public partial class ApplicationSettingsViewModel : SettingsViewModelBase
{
    [NotifyDataErrorInfo] [DirectoryExists("Application Data Path")] [ObservableProperty]
    private string _appDataPath;
    
    [ObservableProperty] private bool _useAppDataPath;
    
    [NotifyDataErrorInfo] [DirectoryExists("Assets Path")] [ObservableProperty]
    private string _assetsPath;
    
    [ObservableProperty] private bool _useAssetsPath;

    [ObservableProperty] private bool _showDeveloperSettings = false;
    
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
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(TransparencyHints))] private EThemeType _theme = EThemeType.Dark;
    
    public ObservableCollection<WindowTransparencyLevel> TransparencyHints => Theme is EThemeType.Mica ? [WindowTransparencyLevel.Mica, WindowTransparencyLevel.AcrylicBlur] : [WindowTransparencyLevel.AcrylicBlur];
    
    public string AssetPath => UseAssetsPath && Directory.Exists(AssetsPath) ? AssetsPath : App.AssetsFolder.FullName;
    
    
    
    public DirectSoundDeviceInfo[] AudioDevices => DirectSoundOut.Devices.ToArray()[1..];

    public async Task BrowseAppDataPath()
    {
        if (await App.BrowseFolderDialog() is { } path) AppDataPath = path;
        
    }
    public async Task BrowseAssetsPath()
    {
        if (await App.BrowseFolderDialog() is { } path) AssetsPath = path;
    }
    
    partial void OnAudioDeviceIndexChanged(int value)
    {
        MusicVM?.UpdateOutputDevice();
        SoundPreviewWM?.UpdateOutputDevice();
    }
    
    partial void OnThemeChanged(EThemeType value)
    {
        if (Avalonia.Application.Current is not { } app) return;

        app.Styles.RemoveAll(style => style is FPStyles);

        var themeUri = new Uri($"avares://FortnitePorting/Assets/Themes/{value.ToString()}Theme.axaml");
        if (AvaloniaXamlLoader.Load(themeUri) is FPStyles newTheme)
            app.Styles.Add(newTheme);
    }
}