using System;
using System.IO;
using FortnitePorting.Launcher.ViewModels;
using Newtonsoft.Json;
using Serilog;

namespace FortnitePorting.Launcher.Application;

public class AppSettings
{
    public static SettingsViewModel Current = new();

    private static readonly DirectoryInfo DirectoryPath = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FortnitePortingLauncher"));
    private static readonly FileInfo FilePath = new(Path.Combine(DirectoryPath.FullName, "AppSettings.json"));

    public static void Load()
    {
        try
        {
            if (!DirectoryPath.Exists) DirectoryPath.Create();
            if (!FilePath.Exists) return;
            Current = JsonConvert.DeserializeObject<SettingsViewModel>(File.ReadAllText(FilePath.FullName)) ??
                      new SettingsViewModel();
            
            Log.Information($"Loaded settings from {FilePath.FullName}");
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
            Current.Profiles = [..ProfilesVM.ProfilesSource.Items];
            
            File.WriteAllText(FilePath.FullName, JsonConvert.SerializeObject(Current, Formatting.Indented));
            Log.Information($"Saved settings to {FilePath.FullName}");
        }
        catch (Exception e)
        {
            Log.Error("Failed to save settings:");
            Log.Error(e.ToString());
        }
    }
}