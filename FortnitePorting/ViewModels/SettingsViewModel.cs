using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using FortnitePorting.Framework;
using FortnitePorting.Services.Endpoints.Models;
using Newtonsoft.Json;

namespace FortnitePorting.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [JsonIgnore] public bool HasValidLocalData => Directory.Exists(LocalArchivePath);
    [JsonIgnore] public bool HasValidCustomData => Directory.Exists(CustomArchivePath) && StringUtils.TryParseAesKey(CustomEncryptionKey, out _);

    [ObservableProperty] private ExportOptionsViewModel exportOptions = new();
    
    // Loading
    [ObservableProperty] private ELoadingType loadingType = ELoadingType.Local;
    [ObservableProperty] private ELanguage language = ELanguage.English;
    [ObservableProperty] private bool loadContentBuilds = true;
    [ObservableProperty] private string localArchivePath;
    [ObservableProperty] private string customArchivePath;
    [ObservableProperty] private string customMappingsPath;
    [ObservableProperty] private string customEncryptionKey = Globals.ZERO_CHAR;
    [ObservableProperty] private EGame customUnrealVersion = EGame.GAME_UE5_3;
    
    // Program
    [ObservableProperty] private bool useFallbackBackground;
    [ObservableProperty] private bool useDiscordRPC = true;
    [ObservableProperty] private string exportPath;
    [ObservableProperty] private bool filterProps = true;
    [ObservableProperty] private bool filterItems = true;
    
    [ObservableProperty] private AuthResponse? epicGamesAuth;
    [ObservableProperty] private List<string> favoritePaths = new();
    
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
            ExportPath = path;
        }
    }
}