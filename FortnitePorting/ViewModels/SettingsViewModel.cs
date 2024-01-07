using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Framework.Extensions;
using FortnitePorting.Services;
using FortnitePorting.Framework.Services;
using FortnitePorting.Framework.ViewModels.Endpoints.Models;
using FortnitePorting.ViewModels.Endpoints.Models;
using FortnitePorting.Views;
using Newtonsoft.Json;

namespace FortnitePorting.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [JsonIgnore] public bool IsRestartRequired;
    [JsonIgnore] public bool PromptedRestartRequired;

    [JsonIgnore] public bool HasValidLocalData => Directory.Exists(LocalArchivePath);
    [JsonIgnore] public bool HasValidCustomData => Directory.Exists(CustomArchivePath) && CustomEncryptionKey.TryParseAesKey(out _);
    [JsonIgnore] public static bool IsWindows11 => Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Build >= 22000;

    [ObservableProperty] private ExportOptionsViewModel exportOptions = new();
    [ObservableProperty] private PluginViewModel plugin = new();

    // Loading
    [ObservableProperty] private ELoadingType loadingType = ELoadingType.Local;
    [ObservableProperty] private ELanguage language = ELanguage.English;
    [ObservableProperty] private bool useCosmeticStreaming = true;
    [ObservableProperty] private string localArchivePath;
    [ObservableProperty] private string customArchivePath;
    [ObservableProperty] private string customMappingsPath;
    [ObservableProperty] private string customEncryptionKey = Globals.ZERO_CHAR;
    [ObservableProperty] private EGame customUnrealVersion = Globals.LatestGameVersion;
    [ObservableProperty] private AesResponse? lastAesResponse;

    // Program
    [ObservableProperty] private DateTime lastUpdateAskTime = DateTime.Now.Subtract(TimeSpan.FromDays(1));
    [ObservableProperty] private FPVersion lastKnownUpdateVersion = Globals.Version;
    
    [ObservableProperty] private bool showConsole = true;
    [ObservableProperty] private EAssetSize assetSize = EAssetSize.Percent100;
    public float AssetSizeMultiplier => AssetSize switch
    {
        EAssetSize.Percent50 => 0.50f,
        EAssetSize.Percent75 => 0.75f,
        EAssetSize.Percent100 => 1.00f,
        EAssetSize.Percent125 => 1.25f,
        EAssetSize.Percent150 => 1.50f,
        EAssetSize.Percent175 => 1.75f,
        EAssetSize.Percent200 => 2.00f,
        _ => 1.0f
    };
    [ObservableProperty] private bool useTabTransition = true;
    [ObservableProperty] private bool useDiscordRPC = true;
    [ObservableProperty] private bool useCustomExportPath;
    [ObservableProperty] private string customExportPath;
    
    // Filtering
    [ObservableProperty] private bool filterProps = true;
    [ObservableProperty] private bool filterItems = true;
    [ObservableProperty] private bool filterTraps = true;

    // Data Storage
    [ObservableProperty] private AuthResponse? epicGamesAuth;
    [ObservableProperty] private List<string> favoritePaths = new();
    [ObservableProperty] private HashSet<string> hiddenFilePaths = new();
    [ObservableProperty] private HashSet<string> hiddenPropPaths = new();
    [ObservableProperty] private HashSet<string> hiddenTrapPaths = new();
    [ObservableProperty] private Dictionary<string, Dictionary<string, string>> itemMeshMappings = new();
    
    // Theming
    [ObservableProperty] private bool useMica = IsWindows11;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(BackgroundColorHex))] private Color backgroundColor = Color.Parse("#1a0038");
    public string BackgroundColorHex => BackgroundColor.ToString();
    [ObservableProperty, NotifyPropertyChangedFor(nameof(AccentColorHex))] private Color accentColor = Color.Parse("#7c3c92");
    public string AccentColorHex => AccentColor.ToString();
    
    [ObservableProperty] private bool useCustomSplashArt;
    [ObservableProperty] private string customSplashArtPath;

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
        nameof(FilterProps),
        nameof(FilterItems),
        nameof(AssetSize)
    };

    public string GetExportPath()
    {
        return UseCustomExportPath && Directory.Exists(CustomExportPath) ? CustomExportPath : AssetsFolder.FullName;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (AppVM is null) return;
        if (MainVM is null) return;
        if (HomeVM is null) return;

        var property = e.PropertyName;
        if (MainVM.ActiveTab is SettingsView && RestartProperties.Contains(property)) IsRestartRequired = true;

        switch (property)
        {
            case nameof(UseDiscordRPC):
            {
                if (UseDiscordRPC)
                {
                    DiscordService.Initialize();
                }
                else
                {
                    DiscordService.Deinitialize();
                }
                break;
            }
            case nameof(UseMica):
            {
                AppVM.UseMicaBackground = UseMica;
                break;
            }
            case nameof(BackgroundColor):
            {
                AppVM.BackgroundColor = BackgroundColor;
                break;
            }
            case nameof(AccentColor):
            {
                ColorExtensions.SetSystemAccentColor(AccentColor);
                break;
            }
            case nameof(UseCustomSplashArt):
            {
                HomeVM.SplashArtSource = UseCustomSplashArt && !string.IsNullOrEmpty(CustomSplashArtPath) ? new Bitmap(CustomSplashArtPath) : HomeVM.DefaultHomeImage;
                break;
            }
            case nameof(CustomSplashArtPath):
            {
                HomeVM.SplashArtSource = UseCustomSplashArt ? new Bitmap(CustomSplashArtPath) : HomeVM.DefaultHomeImage;
                break;
            }
            case nameof(ShowConsole):
            {
                ConsoleExtensions.ToggleConsole(ShowConsole);
                break;
            }
        }
        
        if (IsRestartRequired && !PromptedRestartRequired)
        {
            AppVM.RestartWithMessage("A restart is required.", "An option has been changed that requires a restart to take effect.", mandatory: false);
            IsRestartRequired = false;
            PromptedRestartRequired = true;
        }
    }

    private static readonly FilePickerFileType MappingsFileType = new("Unreal Mappings")
    {
        Patterns = new[] { "*.usmap" }
    };

    public async Task BrowseLocalArchivePath()
    {
        if (await BrowseFolderDialog() is { } path) LocalArchivePath = path;
    }

    public async Task BrowseCustomArchivePath()
    {
        if (await BrowseFolderDialog() is { } path) CustomArchivePath = path;
    }

    public async Task BrowseMappingsFile()
    {
        if (await BrowseFileDialog(MappingsFileType) is { } path) CustomMappingsPath = path;
    }

    public async Task BrowseExportPath()
    {
        if (await BrowseFolderDialog() is { } path) CustomExportPath = path;
    }
    
    public async Task BrowseSplashArtPath()
    {
        if (await BrowseFileDialog() is { } path) CustomSplashArtPath = path;
    }
    
    public async Task ResetSettings()
    {
        AppSettings.Current = new SettingsViewModel();
        AppVM.RestartWithMessage("A restart is required.", "To reset all settings, FortnitePorting must be restarted.");
    }
    
    public async Task OpenLogsFolder()
    {
        Launch(LogsFolder.FullName);
    }
}