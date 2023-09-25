using System;
using System.Collections.Generic;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using FortnitePorting.Services.Endpoints.Models;
using Newtonsoft.Json;

namespace FortnitePorting.Application;

// TODO ADD SUPPORT FOR SETTINGS MIGRATION FOR 1.0 -> 2.0
public class AppSettings
{
    public static AppSettingsContainer Current = new();
    
    private static readonly DirectoryInfo DirectoryPath = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FortnitePorting"));
    private static readonly FileInfo FilePath = new(Path.Combine(DirectoryPath.FullName, "AppSettingsV2.json"));

    public static void Load()
    {
        if (!DirectoryPath.Exists) DirectoryPath.Create();
        if (!FilePath.Exists) return;
        Current = JsonConvert.DeserializeObject<AppSettingsContainer>(File.ReadAllText(FilePath.FullName)) ?? new AppSettingsContainer();
    }
    
    public static void Save()
    {
        File.WriteAllText(FilePath.FullName, JsonConvert.SerializeObject(Current, Formatting.Indented));
    }
}