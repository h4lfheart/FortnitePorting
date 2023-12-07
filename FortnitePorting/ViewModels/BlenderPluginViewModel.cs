using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.Utils;
using FortnitePorting.Controls;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.Framework.Services;
using Serilog;

namespace FortnitePorting.ViewModels;

public partial class BlenderPluginViewModel : ViewModelBase
{
    [ObservableProperty] private bool automaticUpdate = true;
    [ObservableProperty] private ObservableCollection<BlenderInstallInfo> installations = new();
    
    private static readonly FilePickerFileType FileType = new("Blender")
    {
        Patterns = new[] { "blender.exe" }
    };

    public async Task Add()
    {
        var path = await AppVM.BrowseFileDialog(FileType);
        if (path is null) return;
        
        if (CheckBlenderRunning(path)) return;

        var versionInfo = FileVersionInfo.GetVersionInfo(path);
        if (Installations.Any(x => x.BlenderVersion.Equals(versionInfo.ProductVersion))) return;
        var majorVersion = int.Parse(versionInfo.ProductVersion[..1]);
        if (majorVersion < 4)
        {
            MessageWindow.Show("Invalid Blender Version", "Only Blender versions 4.0 or higher are supported.");
            return;
        }

        var installInfo = new BlenderInstallInfo(path, versionInfo.ProductVersion);
        await Sync(installInfo);
        await TaskService.RunDispatcherAsync(() => Installations.Add(installInfo));
    }
    
    public async Task Remove(BlenderInstallInfo removeItem)
    {
        Installations.Remove(removeItem);
        await UnSync(removeItem);
    }

    public async Task SyncAll(bool automatic = false)
    {
        foreach (var blenderInstall in Installations)
        {
            if (CheckBlenderRunning(blenderInstall.BlenderPath, automatic)) break;
            await Sync(blenderInstall, automatic);
        }
    }
    
    public async Task Sync(BlenderInstallInfo installInfo, bool automatic = false)
    {
        var currentPluginVersion = await GetPluginVersion();
        if (installInfo.PluginVersion.Equals(currentPluginVersion) && automatic) return;
        
        var assets = Avalonia.Platform.AssetLoader.GetAssets(new Uri("avares://FortnitePorting/Plugins/Blender"), null);
        foreach (var asset in assets)
        {
            await using var fileStream = File.OpenWrite(Path.Combine(installInfo.AddonPath, asset.AbsolutePath.SubstringAfterLast("/")));
            var assetStream = Avalonia.Platform.AssetLoader.Open(asset);
            await assetStream.CopyToAsync(fileStream);
        }
        installInfo.Update();
        
        Log.Information("Synced Blender {BlenderVersion} Plugin to Version {PluginVersion}", installInfo.BlenderVersion, installInfo.PluginVersion);

        using var blenderProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = installInfo.BlenderPath,
                Arguments = $"-b --python-exit-code 1 --python \"{DependencyService.BlenderScriptFile.FullName}\"",
                UseShellExecute = true
            }
        };
        
        blenderProcess.Exited += (sender, args) =>
        {
            if (automatic) return;
            
            if (sender is Process { ExitCode: 1 } process)
            {
                MessageWindow.Show("An Error Occured", "Blender failed to enable the FortnitePorting plugin. If this is your first time using syncing the plugin, please enable it yourself in the add-ons tab in Blender preferences.");
                Log.Error(process.StandardOutput.ReadToEnd());
            }
        };
        
        blenderProcess.Start();
    }
    
    public async Task UnSync(BlenderInstallInfo installInfo)
    {
        Directory.Delete(installInfo.AddonPath);
    }
    
    public bool CheckBlenderRunning(string path, bool automatic = false)
    {
        // todo kill process button for messageWindow? add ability to append custom buttons to stuff at bottom
        var blenderProcesses = Process.GetProcessesByName("blender");
        var foundProcess = blenderProcesses.FirstOrDefault(process => process.MainModule?.FileName.Equals(path.Replace("/", "\\")) ?? false);
        if (foundProcess is not null && !automatic)
        {
            MessageWindow.Show("Cannot Sync Plugin", $"An instance of blender is open. Please close it to sync the plugin.\n\nPath: \"{path}\"\nPID: {foundProcess.Id}");
            return true;
        }

        return false;
    }

    public async Task<string> GetPluginVersion()
    {
        var initStream = Avalonia.Platform.AssetLoader.Open(new Uri("avares://FortnitePorting/Plugins/Blender/__init__.py"));
        var initText = initStream.ReadToEnd().BytesToString();
        return BlenderInstallInfo.GetPluginVersion(initText);
    }
}

public partial class BlenderInstallInfo : ObservableObject
{
    [ObservableProperty] private string blenderPath;
    [ObservableProperty] private string blenderVersion;
    [ObservableProperty] private string pluginVersion = "???";
    [ObservableProperty] private string addonBasePath;
    [ObservableProperty] private string addonPath;

    public BlenderInstallInfo(string path, string blenderVersion)
    {
        BlenderPath = path;
        BlenderVersion = blenderVersion;
        AddonBasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "Blender Foundation", 
            "Blender", 
            BlenderVersion, 
            "scripts", 
            "addons");
        AddonPath = Path.Combine(AddonBasePath, "FortnitePorting");
        Directory.CreateDirectory(AddonPath);
    }

    public void Update()
    {
        PluginVersion = GetPluginVersion();
    }

    public string GetPluginVersion()
    {
        var initFilepath = Path.Combine(AddonPath, "__init__.py");
        if (!File.Exists(initFilepath)) return PluginVersion;
        
        var initText = File.ReadAllText(initFilepath);
        return GetPluginVersion(initText);
    }

    public static string GetPluginVersion(string text)
    {
        var versionMatch = Regex.Match(text, @"""version"": \((.*)\)");
        return versionMatch.Groups[^1].Value.Replace(", ", ".");
    }
}