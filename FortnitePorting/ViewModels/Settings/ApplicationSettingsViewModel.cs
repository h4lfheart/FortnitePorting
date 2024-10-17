using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Media.Animation;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;
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

    [ObservableProperty] private int _audioDeviceIndex = 0;
    [ObservableProperty] private int _chunkCacheLifetime = 1;
    [ObservableProperty] private bool _downloadDebuggingSymbols;

    [ObservableProperty] private FPVersion _lastOnlineVersion = Globals.Version;

    [ObservableProperty] private bool _useAssetsPath;

    [ObservableProperty] private bool _useTabTransitions = true;
    public string AssetPath => UseAssetsPath && Directory.Exists(AssetsPath) ? AssetsPath : AssetsFolder.FullName;


    [JsonIgnore]
    public NavigationTransitionInfo Transition => UseTabTransitions
        ? new SlideNavigationTransitionInfo()
        : new SuppressNavigationTransitionInfo();
    
    
    public DirectSoundDeviceInfo[] AudioDevices => DirectSoundOut.Devices.ToArray()[1..];


    [RelayCommand]
    public async Task BrowseAssetsPath()
    {
        if (await BrowseFolderDialog() is { } path) AssetsPath = path;
    }

    protected override async void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        switch (e.PropertyName)
        {
            case nameof(DownloadDebuggingSymbols):
            {
                if (ApiVM is null) break;
                
                var executingDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                if (DownloadDebuggingSymbols)
                {
                    var fileNames = await ApiVM.FortnitePorting.GetReleaseFilesAsync();
                    var pdbFiles = fileNames.Where(fileName => fileName.EndsWith(".pdb"));
                    foreach (var pdbFile in pdbFiles) await ApiVM.DownloadFileAsync(pdbFile, executingDirectory);
                }
                else
                {
                    var pdbFiles = executingDirectory.GetFiles("*.pdb");
                    foreach (var pdbFile in pdbFiles) pdbFile.Delete();
                }

                break;
            }
            case nameof(AudioDeviceIndex):
            {
                RadioVM?.UpdateOutputDevice();
                SoundPreviewWM?.UpdateOutputDevice();
                break;
            }
        }
    }
}