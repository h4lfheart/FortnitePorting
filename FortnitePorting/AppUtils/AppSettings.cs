﻿using System;
using System.Collections.Generic;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Versions;
using FortnitePorting.Exports.Blender;
using FortnitePorting.Exports.Unreal;
using FortnitePorting.Models;
using FortnitePorting.Services.Endpoints.Models;
using Newtonsoft.Json;

namespace FortnitePorting.AppUtils;

public partial class AppSettings : ObservableObject
{
    public static AppSettings Current;

    public static readonly DirectoryInfo DirectoryPath = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FortnitePorting"));
    public static readonly DirectoryInfo FilePath = new(Path.Combine(DirectoryPath.FullName, "AppSettings.json"));

    public static void Load()
    {
        if (File.Exists(FilePath.FullName))
        {
            Current = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(FilePath.FullName));
        }

        Current ??= new AppSettings();
    }

    public static void Save()
    {
        File.WriteAllText(FilePath.FullName, JsonConvert.SerializeObject(Current, Formatting.Indented));
    }

    [ObservableProperty] private string archivePath;

    [ObservableProperty] private string mappingPath;

    [ObservableProperty] private string custominstallPath;

    [ObservableProperty] private ELanguage language;

    [ObservableProperty] private EGame ueVersion = EGame.GAME_UE5_2;

    [ObservableProperty] private EInstallType installType;

    [ObservableProperty] private bool discordRichPresence = true;

    [ObservableProperty] private AesResponse? aesResponse;

    [ObservableProperty] private BlenderExportSettings blenderExportSettings = new();

    [ObservableProperty] private UnrealExportSettings unrealExportSettings = new();

    [ObservableProperty] private List<string> favoriteIDs = new();

    [ObservableProperty] private EpicAuthResponse? epicAuth;

    [ObservableProperty] private bool bundleDownloaderEnabled = true;

    [ObservableProperty] private string assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");

    [ObservableProperty] private EUpdateMode updateMode;

    [ObservableProperty] private bool justUpdated = true;

    [ObservableProperty] private DateTime lastUpdateAskTime = DateTime.Now.Subtract(TimeSpan.FromDays(1));

    [ObservableProperty] private bool lightMode;

    [ObservableProperty] private Version lastKnownUpdateVersion;

    [ObservableProperty] private bool showConsole = true;

    [ObservableProperty] private EImageType imageType = EImageType.PNG;

    [ObservableProperty] private float assetSize = 1.0f;

    [ObservableProperty] private Dictionary<string, List<string>> weaponMappings = new();
}