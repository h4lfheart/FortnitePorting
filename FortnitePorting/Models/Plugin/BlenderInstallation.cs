using System;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Shared.Extensions;
using Newtonsoft.Json;
using Tomlyn;

namespace FortnitePorting.Models.Plugin;

public partial class BlenderInstallation(string blenderExecutablePath) : ObservableObject
{
    [ObservableProperty] private string _blenderPath = blenderExecutablePath;
    
    [ObservableProperty] 
    [field: JsonIgnore]
    private Version? _extensionVersion = null;
    
    [ObservableProperty]
    [field: JsonIgnore]
    private string _status = string.Empty;

    public Version BlenderVersion => GetVersion(BlenderPath);

    private string StartupPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Blender Foundation",
        "Blender",
        BlenderVersion.ToString(2),
        "scripts",
        "startup");
    
    private string ManifestPath => Path.Combine(StartupPath,
        "fortnite_porting",
        "blender_manifest.toml");
    
    public static readonly DirectoryInfo PluginWorkingDirectory = new(Path.Combine(App.PluginsFolder.FullName, "Blender"));
    public static readonly Version MinimumVersion = new(4, 2);

    public static Version GetVersion(string blenderPath)
    {
        return new Version(FileVersionInfo.GetVersionInfo(blenderPath).ProductVersion!);
    }

    public bool SyncExtensionVersion()
    {
        if (!File.Exists(ManifestPath))
        {
            Info.Message("Blender Extension", $"Plugin manifest does not exist at path {ManifestPath}, installation may have gone wrong.\nPlease remove the installation from Blender and Fortnite Porting and try again.");
            return false;
        }
        
        var manifestContents = File.ReadAllText(ManifestPath);
        var manifestToml = Toml.ToModel(manifestContents);
        ExtensionVersion = new Version((string) manifestToml["version"]);
        return true;
    }
    
    public void Install(bool verbose = true)
    {
        Status = "Installing";
        
        MiscExtensions.Copy(Path.Combine(PluginWorkingDirectory.FullName, "fortnite_porting"), Path.Combine(StartupPath, "fortnite_porting"));

        var didSyncProperly = SyncExtensionVersion();
        if (verbose)
        {
            if (!didSyncProperly)
            {
                Info.Message("Plugin Installation Failed", 
                    "Failed to install the plugin, please install it manually by dragging and dropping the Fortnite Porting plugin in Blender.", 
                    useButton: true, buttonTitle: "Open Plugins Folder", buttonCommand: () => App.Launch(App.PluginsFolder.FullName));
            }
        }
        
        Status = string.Empty;
    }

    public void Uninstall()
    {
        Status = "Uninstalling";
        
        Directory.Delete(Path.Combine(StartupPath, "fortnite_porting"), true);
    }
}