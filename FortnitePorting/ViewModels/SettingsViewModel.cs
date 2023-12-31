using System;
using System.Collections.Concurrent;
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
using FortnitePorting.Framework.Services;
using FortnitePorting.Framework.ViewModels.Endpoints.Models;
using FortnitePorting.ViewModels.Endpoints.Models;
using FortnitePorting.Views;
using Newtonsoft.Json;

namespace FortnitePorting.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [JsonIgnore] public bool IsRestartRequired;

    [JsonIgnore] public bool HasValidLocalData => Directory.Exists(LocalArchivePath);
    [JsonIgnore] public bool HasValidCustomData => Directory.Exists(CustomArchivePath) && CustomEncryptionKey.TryParseAesKey(out _);

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
    [ObservableProperty] private bool useTabTransition = true;
    [ObservableProperty] private bool useDiscordRPC = true;
    [ObservableProperty] private bool useFallbackBackground;
    [ObservableProperty] private bool useCustomExportPath;
    [ObservableProperty] private string customExportPath;
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

        if (MainVM?.ActiveTab is SettingsView && RestartProperties.Contains(property)) IsRestartRequired = true;

        switch (property)
        {
            case nameof(UseDiscordRPC):
            {
                if (UseDiscordRPC) DiscordService.Initialize();
                else DiscordService.Deinitialize();
                break;
            }
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
}