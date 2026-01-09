using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.ViewModels;
using FortnitePorting.ViewModels.Settings;
using Newtonsoft.Json;
using Serilog;

namespace FortnitePorting.Services;

public partial class SettingsService : ObservableObject, IService
{
    [ObservableProperty] private ExportSettingsViewModel _exportSettings = new();
    [ObservableProperty] private InstallationSettingsViewModel _installation = new();
    [ObservableProperty] private ApplicationSettingsViewModel _application = new();
    [ObservableProperty] private ThemeSettingsViewModel _theme = new();
    [ObservableProperty] private OnlineSettingsViewModel _online = new();
    [ObservableProperty] private PluginViewModel _plugin = new();
    [ObservableProperty] private DebugSettingsViewModel _debug = new();

    public bool ShouldSaveOnExit = true;
    
    public static readonly DirectoryInfo DirectoryPath = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FortnitePorting"));
    public static readonly FileInfo FilePath = new(Path.Combine(DirectoryPath.FullName, "AppSettingsV4.json"));

    public SettingsService()
    {
        DirectoryPath.Create();
    }
    
    public void Load()
    {
        if (!FilePath.Exists) return;
        
        try
        {
            var settings = JsonConvert.DeserializeObject<SettingsService>(File.ReadAllText(FilePath.FullName));
            if (settings is null) return;

            foreach (var property in settings.GetType().GetProperties())
            {
                if (!property.CanWrite) return;
                
                var value = property.GetValue(settings);
                property.SetValue(this, value);
            }
        }
        catch (Exception e)
        {
            Log.Error("Failed to load settings:");
            Log.Error(e.ToString());
        }
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(FilePath.FullName, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        catch (Exception e)
        {
            Log.Error("Failed to save settings:");
            Log.Error(e.ToString());
        }
    }
    
    public void Reset()
    {
        File.Delete(FilePath.FullName);
        ShouldSaveOnExit = false;
    }
}