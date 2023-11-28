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
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Services;
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
        if (CheckBlenderRunning()) return;
        
        var path = await AppVM.BrowseFileDialog(FileType);
        if (path is null) return;

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
        if (CheckBlenderRunning(automatic)) return;
        foreach (var blenderInstall in Installations)
        {
            await Sync(blenderInstall, automatic);
        }
    }
    
    public async Task Sync(BlenderInstallInfo installInfo, bool automatic = false)
    {
        if (CheckBlenderRunning(automatic)) return;
        
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
                UseShellExecute = false,
                RedirectStandardOutput = true
            }
        };
        blenderProcess.Start();
            
        var exitedProperly = blenderProcess.WaitForExit(10000);
        Console.WriteLine(await blenderProcess.StandardOutput.ReadToEndAsync());
        if (!automatic && blenderProcess.ExitCode == 1 || !exitedProperly)
        {
            MessageWindow.Show("An Error Occured", "Blender failed to enable the FortnitePorting plugin. If this is your first time using syncing the plugin, please enable it yourself in the add-ons tab in Blender preferences.");
            Log.Error(await blenderProcess.StandardOutput.ReadToEndAsync());
        }
    }
    
    public async Task UnSync(BlenderInstallInfo installInfo)
    {
        Directory.Delete(installInfo.AddonPath);
    }
    
    public bool CheckBlenderRunning(bool automatic = false)
    {
        var blenderProcesses = Process.GetProcessesByName("blender");
        if (blenderProcesses.Length > 0 && !automatic)
        {
            MessageWindow.Show("Cannot Sync Plugin", "An instance of blender is open. Please close it to sync the plugin.");
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