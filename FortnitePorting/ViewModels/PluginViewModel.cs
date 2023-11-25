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
using FortnitePorting.Framework;
using FortnitePorting.Services;
using Serilog;

namespace FortnitePorting.ViewModels;

// TODO split into blender and unreal specific sections, maybe extra viewmodels?
public partial class PluginViewModel : ViewModelBase
{
    [ObservableProperty] private bool automaticUpdate = true;
    [ObservableProperty] private ObservableCollection<BlenderInstallInfo> blenderInstallations = new();
    
    private static readonly string BlenderEnableScript = """
    import bpy
    bpy.ops.preferences.addon_enable(module='FortnitePorting')
    bpy.ops.wm.save_userpref()
    """;
    private static readonly FilePickerFileType BlenderFileType = new("Blender")
    {
        Patterns = new[] { "blender.exe" }
    };

    public async Task AddBlenderInstallation()
    {
        var path = await AppVM.BrowseFileDialog(BlenderFileType);
        if (path is null) return;

        var versionInfo = FileVersionInfo.GetVersionInfo(path);
        if (BlenderInstallations.Any(x => x.BlenderVersion.Equals(versionInfo.ProductVersion))) return;
        var majorVersion = int.Parse(versionInfo.ProductVersion[..1]);
        if (majorVersion < 4)
        {
            MessageWindow.Show("Invalid Blender Version", "Only Blender versions 4.0 or higher are supported.");
            return;
        }
        
        if (IsBlenderRunning()) return;

        var installInfo = new BlenderInstallInfo(path, versionInfo.ProductVersion);
        await Sync(installInfo);
        await TaskService.RunDispatcherAsync(() => BlenderInstallations.Add(installInfo));
    }
    
    public async Task RemoveBlenderInstallation(BlenderInstallInfo removeItem)
    {
        BlenderInstallations.Remove(removeItem);
        await UnSync(removeItem);
    }
    
    public async Task SyncAll()
    {
        if (IsBlenderRunning()) return;
        foreach (var blenderInstall in BlenderInstallations)
        {
            await Sync(blenderInstall);
        }
    }

    public async Task Sync(BlenderInstallInfo installInfo)
    {
        if (IsBlenderRunning()) return;
        var assets = Avalonia.Platform.AssetLoader.GetAssets(new Uri("avares://FortnitePorting/Plugins/Blender"), null);
        foreach (var asset in assets)
        {
            await using var fileStream = File.OpenWrite(Path.Combine(installInfo.AddonPath, asset.AbsolutePath.SubstringAfterLast("/")));
            var assetStream = Avalonia.Platform.AssetLoader.Open(asset);
            await assetStream.CopyToAsync(fileStream);
        }
        
        using (var blenderProcess = new Process())
        {
            blenderProcess.StartInfo = new ProcessStartInfo
            {
                FileName = installInfo.BlenderPath,
                Arguments = $"-b --python-expr \"{BlenderEnableScript}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            blenderProcess.Start();
            await blenderProcess.WaitForExitAsync();
            Log.Information("egg");
        }

        
        installInfo.Update();
    }
    
    public async Task UnSync(BlenderInstallInfo installInfo)
    {
        Directory.Delete(installInfo.AddonPath);
    }

    public bool IsBlenderRunning()
    {
        var blenderProcesses = Process.GetProcessesByName("blender");
        if (blenderProcesses.Length > 0)
        {
            MessageWindow.Show("Cannot Sync Plugin", "An instance of blender is open. Please close it to sync the plugin.");
            return true;
        }

        return false;
    }
}

public partial class BlenderInstallInfo : ObservableObject
{
    [ObservableProperty] private string blenderPath;
    [ObservableProperty] private string blenderVersion;
    [ObservableProperty] private string pluginVersion = "???";
    [ObservableProperty] private string addonPath;

    public BlenderInstallInfo(string path, string blenderVersion)
    {
        BlenderPath = path;
        BlenderVersion = blenderVersion;
        AddonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "Blender Foundation", 
            "Blender", 
            BlenderVersion, 
            "scripts", 
            "addons",
            "FortnitePorting");
        Directory.CreateDirectory(AddonPath);
        PluginVersion = GetPluginVersion();
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
        var versionMatch = Regex.Match(initText, @"""version"": \((.*)\)");
        if (!versionMatch.Success) return PluginVersion;
        
        return versionMatch.Groups[^1].Value.Replace(", ", ".");
    }
}