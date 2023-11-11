using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.Services.Endpoints.Models;
using FortnitePorting.Views;
using Newtonsoft.Json;
using Serilog;

namespace FortnitePorting.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [JsonIgnore] public bool IsRestartRequired;
    
    [JsonIgnore] public bool HasValidLocalData => Directory.Exists(LocalArchivePath);
    [JsonIgnore] public bool HasValidCustomData => Directory.Exists(CustomArchivePath) && CustomEncryptionKey.TryParseAesKey(out _);

    [ObservableProperty] private ExportOptionsViewModel exportOptions = new();
    
    // Loading
    [ObservableProperty] private ELoadingType loadingType = ELoadingType.Local;
    [ObservableProperty] private ELanguage language = ELanguage.English;
    [ObservableProperty] private bool useCosmeticStreaming = true;
    [ObservableProperty] private string localArchivePath;
    [ObservableProperty] private string customArchivePath;
    [ObservableProperty] private string customMappingsPath;
    [ObservableProperty] private string customEncryptionKey = Globals.ZERO_CHAR;
    [ObservableProperty] private EGame customUnrealVersion = EGame.GAME_UE5_4;
    [ObservableProperty] private AesResponse? lastAesResponse;
    
    // Program
    [ObservableProperty] private bool useTabTransition = true;
    [ObservableProperty] private bool useDiscordRPC = true;
    [ObservableProperty] private bool useFallbackBackground;
    [ObservableProperty] private bool useCustomExportPath;
    [ObservableProperty] private string customExportPath;
    [ObservableProperty] private bool filterProps = true;
    [ObservableProperty] private bool filterItems = true;
    
    [ObservableProperty] private AuthResponse? epicGamesAuth;
    [ObservableProperty] private List<string> favoritePaths = new();
    [ObservableProperty] private HashSet<string> assetsThatArentMesh = new();

    [JsonIgnore] private static readonly string[] RestartProperties = 
    {
        nameof(LoadingType),
        nameof(Language),
        nameof(UseCosmeticStreaming),
        nameof(LocalArchivePath),
        nameof(CustomArchivePath),
        nameof(CustomMappingsPath),
        nameof(CustomEncryptionKey),
        nameof(CustomMappingsPath),
        nameof(UseFallbackBackground),
        nameof(FilterProps),
        nameof(FilterItems)
    };

    public string GetExportPath()
    {
        return UseCustomExportPath && Directory.Exists(CustomExportPath) ? CustomExportPath : App.AssetsFolder.FullName;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        var property = e.PropertyName;

        if (MainVM?.ActiveTab is SettingsView && RestartProperties.Contains(property))
        {
            IsRestartRequired = true;
        }

        if (UseDiscordRPC)
        {
            DiscordService.Initialize();
        }
        else
        {
            DiscordService.Deinitialize();
        }

        switch (property)
        {
            case nameof(UseTabTransition):
            {
                if (MainVM is null) break;
                MainVM.AnimateTabChanges = UseTabTransition;
                break;
            }
        }
    }

    private static readonly FilePickerFileType MappingsFileType = new("Unreal Mappings")
    {
        Patterns = new[] { "*.usmap" }
    };
    
    [RelayCommand]
    private async Task BrowseLocalArchivePath()
    {
        if (await AppVM.BrowseFolderDialog() is {} path)
        {
            LocalArchivePath = path;
        }
    }
    
    [RelayCommand]
    private async Task BrowseCustomArchivePath()
    {
        if (await AppVM.BrowseFolderDialog() is {} path)
        {
            CustomArchivePath = path;
        }
    }

    [RelayCommand]
    private async Task BrowseMappingsFile()
    {
        if (await AppVM.BrowseFileDialog(MappingsFileType) is {} path)
        {
            CustomMappingsPath = path;
        }
    }
    
    [RelayCommand]
    private async Task BrowseExportPath()
    {
        if (await AppVM.BrowseFolderDialog() is {} path)
        {
            CustomExportPath = path;
        }
    }
}