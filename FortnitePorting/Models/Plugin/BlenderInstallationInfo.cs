using System;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Tomlyn;

namespace FortnitePorting.Models.Plugin;

public partial class BlenderInstallationInfo : ObservableObject
{
    [ObservableProperty] private string _blenderPath;
    [ObservableProperty] private Version _extensionVersion;
    [ObservableProperty, JsonIgnore] private string _status;

    public Version BlenderVersion => new(FileVersionInfo.GetVersionInfo(BlenderPath).ProductVersion!);

    public string ManifestPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "Blender Foundation", 
        "Blender", 
        BlenderVersion.ToString(2), 
        "extensions", 
        "user_default",
        "fortnite_porting",
        "blender_manifest.toml");

    public BlenderInstallationInfo(string blenderPath)
    {
        BlenderPath = blenderPath;
    }

    public void SyncExtensionVersion()
    {
        var manifestContents = File.ReadAllText(ManifestPath);
        var manifestToml = Toml.ToModel(manifestContents);
        ExtensionVersion = new Version((string) manifestToml["version"]);
    }
    
}