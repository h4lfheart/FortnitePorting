using System.Collections.Generic;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using FortnitePorting.Services.Endpoints.Models;
using Newtonsoft.Json;

namespace FortnitePorting.Application;

public partial class AppSettingsContainer : ObservableObject
{
    [JsonIgnore] public bool HasValidLocalData => Directory.Exists(LocalArchivePath);
    [JsonIgnore] public bool HasValidCustomData => Directory.Exists(CustomArchivePath) && CustomEncryptionKey.TryParseAesKey(out _);
    
    // Loading
    [ObservableProperty] private ELoadingType loadingType = ELoadingType.Local;
    [ObservableProperty] private string localArchivePath = string.Empty;
    [ObservableProperty] private string customArchivePath = string.Empty;
    [ObservableProperty] private string customMappingsPath = string.Empty;
    [ObservableProperty] private string customEncryptionKey = Globals.ZERO_CHAR;
    [ObservableProperty] private EGame customUnrealVersion = EGame.GAME_UE5_3;
    
    // Program
    [ObservableProperty] private bool useFallbackBackground;
    [ObservableProperty] private bool useDiscordRPC = true;
    [ObservableProperty] private bool loadContentBuilds = true;
    [ObservableProperty] private AuthResponse? epicGamesAuth;
    [ObservableProperty] private ELanguage language = ELanguage.English;
    [ObservableProperty] private List<string> favoritePaths = new();
}