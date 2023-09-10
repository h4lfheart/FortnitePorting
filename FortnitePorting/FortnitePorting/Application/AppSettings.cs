using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace FortnitePorting.Application;

// TODO ADD SUPPORT FOR SETTINGS MIGRATION FOR 1.0 -> 2.0
public partial class AppSettings : ObservableObject
{
    public static AppSettings Current = new();
    
    public static readonly DirectoryInfo DirectoryPath = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FortnitePorting"));
    public static readonly FileInfo FilePath = new(Path.Combine(DirectoryPath.FullName, "AppSettingsV2.json"));

    public static void Load()
    {
        if (!FilePath.Exists) return;
        Current = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(FilePath.FullName)) ?? new AppSettings();
    }
    
    public static void Save()
    {
        File.WriteAllText(FilePath.FullName, JsonConvert.SerializeObject(Current, Formatting.Indented));
    }
    
    [ObservableProperty] private string localArchivePath = string.Empty;
    [ObservableProperty] private string customArchivePath = string.Empty;
    [ObservableProperty] private string customMappingsPath = string.Empty;
    [ObservableProperty] private string customEncryptionKey = Globals.ZERO_CHAR;
    [ObservableProperty] private EGame customUnrealVersion = EGame.GAME_UE5_3;
    
    [ObservableProperty] private bool useDiscordRPC = true;
}