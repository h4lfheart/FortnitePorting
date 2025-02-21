using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    [ObservableProperty] private ObservableCollection<BlenderInstallation> _installations = [];
    [ObservableProperty, JsonIgnore] private int _selectedInstallationIndex = 0;

    public override async Task Initialize()
    {
        if (!BlenderInstallation.PluginWorkingDirectory.Exists)
            BlenderInstallation.PluginWorkingDirectory.Create();

        foreach (var installation in Installations.ToArray())
        {
            if (installation.SyncExtensionVersion()) continue;
            
            installation.Uninstall();
            Installations.Remove(installation);
        }
    }


    public async Task AddInstallation()
    {
        if (await BrowseFileDialog(fileTypes: Globals.BlenderFileType) is not { } blenderPath) return;

        var blenderVersion = BlenderInstallation.GetVersion(blenderPath);
        if (Installations.Any(existing => existing.BlenderVersion == blenderVersion))
        {
            AppWM.Message("Blender Extension", $"The plugin for Blender {blenderVersion} has already been installed.", InfoBarSeverity.Warning);
            return;
        }
        
        if (blenderVersion < BlenderInstallation.MinimumVersion)
        {
            AppWM.Message("Blender Extension", 
                $"Blender {blenderVersion} is too low of a version. Only Blender {BlenderInstallation.MinimumVersion} and higher are supported.", 
                InfoBarSeverity.Error, autoClose: false);
            return;
        }
        
        if (TryGetBlenderProcess(blenderPath, out var blenderProcess))
        {
            AppWM.Message("Failed to Add Blender Installation", 
                $"This version of blender is currently open. Please close it and re-add the installation.", 
                InfoBarSeverity.Error, autoClose: false, 
                useButton: true, buttonTitle: "Kill Blender Process", buttonCommand: () =>
                {
                    blenderProcess.Kill(entireProcessTree: true);
                });
            return;
        }

        var installation = new BlenderInstallation(blenderPath);
        
        Installations.Add(installation);
        SelectedInstallationIndex = Installations.Count - 1;

        await TaskService.RunAsync(() =>
        {
            installation.Install();
        });
    }

    public async Task RemoveInstallation()
    {
        if (Installations.Count == 0) return;
        
        await TaskService.RunAsync(() =>
        {
            Installations[SelectedInstallationIndex].Uninstall();
        });
        
        var selectedIndexToRemove = SelectedInstallationIndex;
        Installations.RemoveAt(selectedIndexToRemove);
        SelectedInstallationIndex = selectedIndexToRemove == 0 ? 0 : selectedIndexToRemove - 1;
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
            installation.Install();

            AppWM.Message("Blender Extension", $"Successfully updated the Blender {installation.BlenderVersion} extension from {previousVersion} to {currentVersion}");
        }
    }

    private static bool TryGetBlenderProcess(string path, [MaybeNullWhen(false)] out Process process)
    {
        var blenderProcesses = Process.GetProcessesByName("blender");
        process = blenderProcesses.FirstOrDefault(process => process.MainModule is { } mainModule && mainModule.FileName.Equals(path.Replace("/", "\\")));
        return process is not null;
    }
}