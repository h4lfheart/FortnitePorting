using System;
using System.IO;
using FortnitePorting.ViewModels;
using Newtonsoft.Json;
using Serilog;

namespace FortnitePorting.Application;

public class AppSettings
{
    public static SettingsViewModel Current = new();

    private static readonly DirectoryInfo DirectoryPath = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FortnitePorting"));
    private static readonly FileInfo FilePath = new(Path.Combine(DirectoryPath.FullName, "AppSettingsV3.json"));

    public static void Load()
    {
        try
        {
            if (!DirectoryPath.Exists) DirectoryPath.Create();
            if (!FilePath.Exists) return;
            Current = JsonConvert.DeserializeObject<SettingsViewModel>(File.ReadAllText(FilePath.FullName)) ??
                      new SettingsViewModel();
        }
        catch (Exception e)
        {
            Log.Error("Failed to load settings:");
            Log.Error(e.ToString());
        }
    }

    public static void Save()
    {
        try
        {
            
            File.WriteAllText(FilePath.FullName, JsonConvert.SerializeObject(Current, Formatting.Indented));
        }
        catch (Exception e)
        {
            Log.Error("Failed to save settings:");
            Log.Error(e.ToString());
        }
    }
}