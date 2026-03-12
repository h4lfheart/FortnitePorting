using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.ViewModels;
using Newtonsoft.Json;
using Tomlyn;

namespace FortnitePorting.Models.Plugin;

public partial class BlenderInstallation(string blenderExecutablePath) : ObservableObject
{
    [ObservableProperty] private string _blenderPath = blenderExecutablePath;
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ExtensionVersionString))] 
    [property: JsonIgnore]
    private Version? _extensionVersion = null;

    [JsonIgnore]
    public string ExtensionVersionString => ExtensionVersion is null ? string.Empty : $"v{ExtensionVersion.ToString()}";
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(StatusBrush))]
    [property: JsonIgnore]
    private EPluginStatusType _status = EPluginStatusType.Newest;

    [JsonIgnore]
    public SolidColorBrush StatusBrush => Status switch
    {
        EPluginStatusType.Newest => SolidColorBrush.Parse("#17854F"),
        EPluginStatusType.UpdateAvailable => SolidColorBrush.Parse("#E0A100"),
        EPluginStatusType.Failed => SolidColorBrush.Parse("#A61717"),
        EPluginStatusType.Modifying => SolidColorBrush.Parse("#6F6F75"),
    };
    
    [JsonIgnore]
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
    public static readonly Version MinimumVersion = new(5, 0);

    public static Version GetVersion(string blenderPath)
    {
        return new Version(FileVersionInfo.GetVersionInfo(blenderPath).ProductVersion!);
    }

    public bool SyncExtensionVersion()
    {
        if (!File.Exists(ManifestPath))
        {
            Info.Message("Blender Extension", $"Plugin manifest does not exist at path {ManifestPath}, installation may have gone wrong.\nPlease remove the installation from Fortnite Porting and try again.");
            Status = EPluginStatusType.Failed;
            return false;
        }
        
        var manifestContents = File.ReadAllText(ManifestPath);
        var manifestToml = Toml.ToModel(manifestContents);
        ExtensionVersion = new Version((string) manifestToml["version"]);

        var fpExtensionVersion = new FPVersion(ExtensionVersion.Major, ExtensionVersion.Minor, ExtensionVersion.Build);
        Status = fpExtensionVersion.Equals(Globals.Version)
            ? EPluginStatusType.Newest
            : EPluginStatusType.UpdateAvailable;
        
        return true;
    }
    
    public void Install(bool verbose = true)
    {
        Status = EPluginStatusType.Modifying;
        
        MiscExtensions.Copy(Path.Combine(PluginWorkingDirectory.FullName, "fortnite_porting"), Path.Combine(StartupPath, "fortnite_porting"));

        var didSyncProperly = SyncExtensionVersion();
        if (verbose)
        {
            if (!didSyncProperly)
            {
                Info.Message("Plugin Installation Failed", 
                    "Failed to install the plugin, please install it manually by dragging and dropping the Fortnite Porting plugin in Blender.", 
                    useButton: true, buttonTitle: "Open Plugins Folder", buttonCommand: () => App.Launch(App.PluginsFolder.FullName));

                Status = EPluginStatusType.Failed;
            }
        }

        Status = EPluginStatusType.Newest;
    }

    public void Uninstall()
    {
        Status = EPluginStatusType.Modifying;
        
        Directory.Delete(Path.Combine(StartupPath, "fortnite_porting"), true);
    }

    public async Task Launch()
    {
        App.Launch(BlenderPath);
    }
}