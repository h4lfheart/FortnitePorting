using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Shared.Services;
using Newtonsoft.Json;
using Serilog;
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
    private string ManifestPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "Blender Foundation", 
        "Blender", 
        BlenderVersion.ToString(2), 
        "extensions", 
        "user_default",
        "fortnite_porting",
        "blender_manifest.toml");
    
    public static readonly DirectoryInfo PluginWorkingDirectory = new(Path.Combine(PluginsFolder.FullName, "Blender"));
    public static readonly Version MinimumVersion = new(4, 2);

    public static Version GetVersion(string blenderPath)
    {
        return new Version(FileVersionInfo.GetVersionInfo(blenderPath).ProductVersion!);
    }

    public bool SyncExtensionVersion()
    {
        if (!File.Exists(ManifestPath))
        {
            AppWM.Message("Blender Extension", $"Plugin manifest does not exist at path {ManifestPath}, installation may have gone wrong.\nPlease remove the installation from Blender and Fortnite Porting and try again.");
            return false;
        }
        
        var manifestContents = File.ReadAllText(ManifestPath);
        var manifestToml = Toml.ToModel(manifestContents);
        ExtensionVersion = new Version((string) manifestToml["version"]);
        return true;
    }
    
    // TODO popup for enabling plugin
    public void Install()
    {
        Status = "Installing";
        
        var ueFormatZip = BuildPlugin("io_scene_ueformat");
        var fnPortingZip = BuildPlugin("fortnite_porting");

        InstallPlugin(ueFormatZip);
        InstallPlugin(fnPortingZip);

        SyncExtensionVersion();
        
        Status = string.Empty;
    }

    public void Uninstall()
    {
        Status = "Uninstalling";
        RemovePlugin("fortnite_porting");
        RemovePlugin("io_scene_ueformat");
    }

    private string BuildPlugin(string name)
    {
        Status = $"Building {name}";
        
        var outPath = Path.Combine(PluginWorkingDirectory.FullName, $"{name}.zip");
        BlenderExtensionCommand("build", $"--output-filepath \"{outPath}\" --verbose", workingDirectory: Path.Combine(PluginWorkingDirectory.FullName, name));
        return outPath;
    }

    private void InstallPlugin(string zipPath)
    {
        Status = $"Installing {Path.GetFileName(zipPath)}";
        BlenderExtensionCommand("install-file", $"\"{zipPath}\" -r user_default");
    }

    private void RemovePlugin(string name)
    {
        BlenderExtensionCommand("remove", name);
    }

    private void BlenderExtensionCommand(string command, string args, string workingDirectory = "")
    {
        using var buildProcess = new Process();
        buildProcess.StartInfo = new ProcessStartInfo
        {
            FileName = BlenderPath,
            Arguments = $"--command extension {command} {args}",
            WorkingDirectory = workingDirectory,
            UseShellExecute = true,
        };

        Log.Information($"Executing {BlenderPath} {command} {args}");
        buildProcess.Start();
        buildProcess.WaitForExit();
    }
    
}