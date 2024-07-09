using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.Utils;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Models.Plugin;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using Newtonsoft.Json;
using Serilog;

namespace FortnitePorting.ViewModels.Plugin;

public partial class BlenderPluginViewModel : ViewModelBase
{
    [ObservableProperty] private bool _automaticallySync = true;
    [ObservableProperty] private ObservableCollection<BlenderInstallationInfo> _installations = [];
    [ObservableProperty, JsonIgnore] private int _selectedInstallationIndex = 0;

    private static readonly DirectoryInfo BlenderRoot = new(Path.Combine(PluginsFolder.FullName, "Blender"));
    private static readonly Version MinimumVersion = new(4, 2);

    public override async Task Initialize()
    {
        if (!BlenderRoot.Exists)
            BlenderRoot.Create();

        SyncExtensionVersions();

    }

    public void SyncExtensionVersions()
    {
        foreach (var installation in Installations)
        {
            installation.SyncExtensionVersion();
        }
    }

    public async Task AddInstallation()
    {
        if (await BrowseFileDialog(fileTypes: Globals.BlenderFileType) is not { } blenderPath) return;

        if (TryGetBlenderProcess(blenderPath, out var blenderProcess))
        {
            AppWM.Message("Failed to Add Blender Installation", 
                $"This version of blender is currently open. Please close it and re-add the installation.\nPath: {blenderPath}\nPID: {blenderProcess.Id}", 
                InfoBarSeverity.Error, autoClose: false);
            return;
        }

        var installationInfo = new BlenderInstallationInfo(blenderPath);
        if (Installations.Any(installation => installation.BlenderVersion == installationInfo.BlenderVersion)) return;
        if (installationInfo.BlenderVersion < MinimumVersion)
        {
            AppWM.Message("Blender Extension", 
                $"Blender {installationInfo.BlenderVersion} is too low of a version. Only Blender {MinimumVersion} and higher are supported.", 
                InfoBarSeverity.Error, autoClose: false);
        }
        
        Installations.Add(installationInfo);
        SelectedInstallationIndex = Installations.Count - 1;

        await Sync(installationInfo);
    }

    public async Task RemoveInstallation()
    {
        await TaskService.RunAsync(() =>
        {
            var installation = Installations[SelectedInstallationIndex];
            installation.Status = "(Removing)";
            InstallPlugin("io_scene_ueformat", installation.BlenderPath);
            InstallPlugin("fortnite_porting", installation.BlenderPath);

            var selectedIndexToRemove = SelectedInstallationIndex;
            Installations.RemoveAt(selectedIndexToRemove);
            SelectedInstallationIndex = selectedIndexToRemove == 0 ? 0 : selectedIndexToRemove - 1;
        });
    }

    public async Task SyncInstallations()
    {
        await SyncInstallations(true);
    }
    
    public async Task SyncInstallations(bool verbose)
    {
        var currentVersion = Globals.Version.ToVersion();
        foreach (var installation in Installations)
        {
            installation.SyncExtensionVersion();
            if (TryGetBlenderProcess(installation.BlenderPath, out var blenderProcess))
            {
                if (verbose)
                {
                    AppWM.Message("Blender Extension", 
                        $"Blender {installation.BlenderVersion} is currently open. Please close it and re-sync the installation.\nPath: {installation.BlenderPath}\nPID: {blenderProcess.Id}", 
                        InfoBarSeverity.Error, autoClose: false);
                }
                
                continue;
            }

            if (currentVersion == installation.ExtensionVersion)
            {
                if (verbose)
                {
                    AppWM.Message("Blender Extension", $"Blender {installation.BlenderVersion} is already up to date.");
                }
                
                continue;
            }

            var previousVersion = installation.ExtensionVersion;
            await Sync(installation);

            AppWM.Message("Blender Extension", $"Successfully updated the Blender {installation.BlenderVersion} extension from {previousVersion} to {currentVersion}");
        }
    }
    public async Task Sync(BlenderInstallationInfo installation)
    {
        await TaskService.Run(() =>
        {
            installation.Status = "(Syncing)";

            var ueFormatZip = BuildPlugin("io_scene_ueformat", installation.BlenderPath);
            var fnPortingZip = BuildPlugin("fortnite_porting", installation.BlenderPath);

            InstallPlugin(ueFormatZip, installation.BlenderPath);
            InstallPlugin(fnPortingZip, installation.BlenderPath);

            installation.SyncExtensionVersion();
            installation.Status = string.Empty;
        });
    }

    public string BuildPlugin(string name, string blenderPath)
    {
        BlenderExtensionCommand("build", $"--output-dir \"{BlenderRoot.FullName}\"", blenderPath,
            workingDirectory: Path.Combine(BlenderRoot.FullName, name));
        return Path.Combine(BlenderRoot.FullName, $"{name}.zip");
    }
    
    public void InstallPlugin(string zipPath, string blenderPath)
    {
        BlenderExtensionCommand("install-file", $"\"{zipPath}\" -r user_default -e", blenderPath);
    }
    
    public void RemovePlugin(string name, string blenderPath)
    {
        BlenderExtensionCommand("remove", name, blenderPath);
    }

    public void BlenderExtensionCommand(string command, string args, string blenderPath, string workingDirectory = "")
    {
        using var buildProcess = new Process();
        buildProcess.StartInfo = new ProcessStartInfo
        {
            FileName = blenderPath,
            Arguments = $"--command extension {command} {args}",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
            

        buildProcess.Start();
        buildProcess.WaitForExit();
        
        var output = buildProcess.StandardOutput.ReadToEnd();
        var lockMatch = Regex.Match(output, "Error: Lock exists: lock is held by other session: (.*)");
        Log.Information(lockMatch.Groups.Count.ToString());
        if (lockMatch.Groups.Count > 1)
        {
            AppWM.Message("Blender Extension", $"A lock has been put on the user_default extension repository. Please delete \"{lockMatch.Groups[1].Value.Trim()}\" and try again.");
        }
        
        Console.WriteLine($"------- {blenderPath} -------");
        Console.WriteLine(output);
    }

    public bool TryGetBlenderProcess(string path, out Process process)
    {
        var blenderProcesses = Process.GetProcessesByName("blender");
        process = blenderProcesses.FirstOrDefault(process => process.MainModule is { } mainModule && mainModule.FileName.Equals(path.Replace("/", "\\")));
        return process is not null;

    }
}