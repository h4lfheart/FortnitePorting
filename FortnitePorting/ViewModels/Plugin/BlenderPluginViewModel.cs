using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Models.Information;
using FortnitePorting.Models.Plugin;
using FortnitePorting.Services;

namespace FortnitePorting.ViewModels.Plugin;

public partial class BlenderPluginViewModel : PluginInstallationViewModelBase<BlenderInstallation>
{
    [ObservableProperty] private bool _completedFirstInstall;

    protected override DirectoryInfo PluginWorkingDirectory => BlenderInstallation.PluginWorkingDirectory;

    protected override bool TrySyncVersion(BlenderInstallation installation) => installation.SyncExtensionVersion();

    protected override void Uninstall(BlenderInstallation installation) => installation.Uninstall();

    public override async Task AddInstallation()
    {
        if (await App.BrowseFileDialog(fileTypes: Globals.BlenderFileType) is not { } blenderPath) return;

        var blenderVersion = BlenderInstallation.TryGetVersion(blenderPath);
        if (blenderVersion is null)
        {
            Info.Message("Blender Extension", "Could not determine the Blender version from the selected file. Please check your Blender installation.", InfoBarSeverity.Error, autoClose: false);
            return;
        }

        if (Installations.Any(existing => existing.BlenderVersion == blenderVersion))
        {
            Info.Message("Blender Extension", $"The plugin for Blender {blenderVersion} has already been installed.", InfoBarSeverity.Warning);
            return;
        }

        if (blenderVersion < BlenderInstallation.MinimumVersion)
        {
            Info.Message("Blender Plugin",
                $"Blender {blenderVersion} is too low of a version. Only Blender versions {BlenderInstallation.MinimumVersion} and higher are supported.",
                InfoBarSeverity.Error, autoClose: false);
            return;
        }

        if (blenderVersion < BlenderInstallation.MinimumModernVersion)
        {
            Info.Dialog("Legacy Blender Plugin",
                "You are using a legacy version of the blender plugin. Modern V4 features such as the modular material system will not be supported.",
                canClose: false,
                buttons: [ 
                    new DialogButton
                    {
                        Text = "I Understand"
                    }
                ]);
        }

        if (TryGetBlenderProcess(blenderPath, out var blenderProcess))
        {
            Info.Message("Failed to Add Blender Installation",
                $"This version of Blender is currently open. Please close it and re-add the installation.",
                InfoBarSeverity.Error, autoClose: false,
                useButton: true, buttonTitle: "Kill Blender Process", buttonCommand: () =>
                {
                    blenderProcess.Kill(entireProcessTree: true);
                });
            return;
        }

        var installation = new BlenderInstallation(blenderPath);

        Installations.Add(installation);

        await TaskService.RunAsync(() =>
        {
            installation.Install();

            if (!CompletedFirstInstall)
            {
                Info.Message("Blender Plugin", "In Fortnite Porting V4, you no longer need to enable the plugin in Blender. The plugin should now be working as is and you are free to continue!", autoClose: false);
                CompletedFirstInstall = true;
            }
        });
    }

    public override async Task SyncInstallations(bool verbose)
    {
        var currentVersion = Globals.Version.ToVersion();
        foreach (var installation in Installations)
        {
            installation.SyncExtensionVersion();

            if (installation.BlenderVersion is null)
            {
                if (verbose)
                {
                    Info.Message("Blender Extension",
                        $"Could not determine Blender version for installation at {installation.BlenderPath}. Skipping.",
                        InfoBarSeverity.Error, autoClose: false);
                }

                continue;
            }

            if (TryGetBlenderProcess(installation.BlenderPath, out var blenderProcess))
            {
                if (verbose)
                {
                    Info.Message("Blender Extension",
                        $"Blender {installation.BlenderVersion} is currently open. Please close it and re-sync the installation.\nPath: {installation.BlenderPath}\nPID: {blenderProcess.Id}",
                        InfoBarSeverity.Error, autoClose: false);
                }

                continue;
            }

            if (currentVersion == installation.ExtensionVersion)
            {
                if (verbose)
                {
                    Info.Message("Blender Extension", $"Blender {installation.BlenderVersion} is already up to date, syncing anyways.");
                }

                installation.Install(verbose);

                continue;
            }

            var previousVersion = installation.ExtensionVersion;
            installation.Install(verbose);

            if (verbose)
            {
                Info.Message("Blender Extension", $"Successfully updated the Blender {installation.BlenderVersion} extension from {previousVersion} to {currentVersion}");
            }
        }
    }

    private static bool TryGetBlenderProcess(string path, [MaybeNullWhen(false)] out Process process)
    {
        var blenderProcesses = Process.GetProcessesByName("blender");
        process = blenderProcesses.FirstOrDefault(process => process.MainModule is { } mainModule && mainModule.FileName.Equals(path.Replace("/", "\\")));
        return process is not null;
    }
}
