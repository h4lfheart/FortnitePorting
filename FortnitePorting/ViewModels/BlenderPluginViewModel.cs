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
using FortnitePorting.Framework.Controls;
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
        var path = await BrowseFileDialog(FileType);
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
        try
        {
            foreach (var blenderInstall in Installations)
            {
                await Sync(blenderInstall, automatic);
            }
        }
        catch (Exception e)
        {
            HandleException(e);
        }
    }
    
    public async Task Sync(BlenderInstallInfo installInfo, bool automatic = false)
    {
        installInfo.Update();
        
        var currentPluginVersion = await GetPluginVersion();
        if (CheckBlenderRunning(installInfo.BlenderPath, automatic))
        {
            if (!installInfo.PluginVersion.Equals(currentPluginVersion) && automatic)
            {
                MessageWindow.Show("An Error Occurred", $"FortnitePorting tried to auto sync the plugin, but an instance of blender is open. Please close it and sync the plugin in the plugin tab.");
            }
            return;
        }
        
        if (installInfo.PluginVersion.Equals(currentPluginVersion) && automatic) return;
        
        var assets = Avalonia.Platform.AssetLoader.GetAssets(new Uri("avares://FortnitePorting/Plugins/Blender"), null);
        foreach (var asset in assets)
        {
            var filePath = Path.Combine(installInfo.AddonBasePath, asset.AbsolutePath.Replace("/Plugins/Blender/", string.Empty));
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            
            var assetStream = Avalonia.Platform.AssetLoader.Open(asset);
            await File.WriteAllBytesAsync(filePath, assetStream.ReadToEnd());
        }
        installInfo.Update();
        
        Log.Information("Synced Blender {BlenderVersion} Plugin to Version {PluginVersion}", installInfo.BlenderVersion, installInfo.PluginVersion);
        MessageWindow.Show("Sync Successful", $"Successfully synced the Blender {installInfo.BlenderVersion} plugin to version {installInfo.PluginVersion}. If this is your first time installing the plugin, please ensure the plugin is enabled in Blender.");

        try
        {
            using var blenderProcess = new Process();
            blenderProcess.StartInfo = new ProcessStartInfo
            {
                FileName = installInfo.BlenderPath,
                Arguments = $"-b --python-exit-code 1 --python \"{DependencyService.BlenderScriptFile.FullName}\"",
                UseShellExecute = false
            };

            blenderProcess.Exited += (sender, args) =>
            {
                if (automatic) return;
                
                if (sender is Process { ExitCode: 1 } process)
                {
                    MessageWindow.Show("An Error Occured", "Blender failed to enable the FortnitePorting plugin. If this is your first time using syncing the plugin, please enable it yourself in the add-ons tab in Blender preferences, otherwise, you may ignore this message.");
                    Log.Error(process.StandardOutput.ReadToEnd());
                }
            };

            blenderProcess.Start();
        }
        catch (Exception e)
        {
            MessageWindow.Show("An Error Occured", "Blender failed to enable the FortnitePorting plugin. If this is your first time using syncing the plugin, please enable it yourself in the add-ons tab in Blender preferences, otherwise, you may ignore this message.");
            Log.Error(e.ToString());
        }
      
    }
    
    public async Task UnSync(BlenderInstallInfo installInfo)
    {
        Directory.Delete(Path.Combine(installInfo.AddonBasePath, "FortnitePorting"));
        Directory.Delete(Path.Combine(installInfo.AddonBasePath, "io_scene_ueformat"));
    }
    
    public bool CheckBlenderRunning(string path, bool automatic = false)
    {
        var blenderProcesses = Process.GetProcessesByName("blender");
        var foundProcess = blenderProcesses.FirstOrDefault(process => process.MainModule?.FileName.Equals(path.Replace("/", "\\")) ?? false);
        if (foundProcess is not null)
        {
            if (!automatic)
            {
                MessageWindow.Show("Cannot Sync Plugin", 
                    $"An instance of blender is open. Please close it to sync the plugin.\n\nPath: \"{path}\"\nPID: {foundProcess.Id}",
                    buttons: [new MessageWindowButton("Kill Process", window =>
                    {
                        foundProcess.Kill();
                        window.Close();
                    }), MessageWindowButtons.Continue]);
            }
           
            return true;
        }

        return false;
    }

    public async Task<string> GetPluginVersion()
    {
        var initStream = Avalonia.Platform.AssetLoader.Open(new Uri("avares://FortnitePorting/Plugins/Blender/FortnitePorting/__init__.py"));
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
    }

    public void Update()
    {
        PluginVersion = GetPluginVersion();
    }

    public string GetPluginVersion()
    {
        var initFilepath = Path.Combine(AddonBasePath, "FortnitePorting", "__init__.py");
        if (!File.Exists(initFilepath)) return PluginVersion;
        
        var initText = File.ReadAllText(initFilepath);
        return GetPluginVersion(initText);
    }

    public static string GetPluginVersion(string text)
    {
        var versionMatch = Regex.Match(text, @"""version"": \((.*)\)");
        return versionMatch.Groups[^1].Value.Replace(", ", ".").Replace("\"", string.Empty);
    }
}