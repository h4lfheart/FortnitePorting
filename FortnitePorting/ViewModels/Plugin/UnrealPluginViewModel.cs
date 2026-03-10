using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Models.Plugin;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using Newtonsoft.Json;

namespace FortnitePorting.ViewModels.Plugin;

public partial class UnrealPluginViewModel : ViewModelBase
{
    [ObservableProperty] private bool _automaticallySync = true;
    [ObservableProperty] private ObservableCollection<UnrealInstallation> _installations = [];

    public override async Task Initialize()
    {
        if (!UnrealInstallation.PluginWorkingDirectory.Exists)
            UnrealInstallation.PluginWorkingDirectory.Create();

        foreach (var installation in Installations.ToArray())
        {
            if (installation.SyncVersion()) continue;

            installation.Uninstall();
            Installations.Remove(installation);
        }
    }

    public async Task AddInstallation()
    {
        if (await App.BrowseFileDialog(fileTypes: Globals.UnrealProjectFileType) is not { } projectPath) return;

        if (Installations.Any(existing => existing.ProjectFilePath == projectPath))
        {
            Info.Message("Unreal Plugin", "This project has already been added.", InfoBarSeverity.Warning);
            return;
        }

        var installation = new UnrealInstallation(projectPath);
        Installations.Add(installation);

        await TaskService.RunAsync(() =>
        {
            installation.Install();
        });
    }

    public async Task RemoveInstallation(UnrealInstallation installation)
    {
        TaskService.Run(() =>
        {
            installation.Uninstall();
            Installations.Remove(installation);
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
            installation.SyncVersion();

            if (currentVersion == installation.Version)
            {
                if (verbose)
                    Info.Message("Unreal Plugin", $"{installation.Name} is already up to date.");

                continue;
            }

            var previousVersion = installation.Version;
            installation.Install(verbose);

            if (verbose)
            {
                Info.Message("Unreal Plugin",
                    $"Successfully updated {installation.Name} from {previousVersion} to {currentVersion}");
            }
        }
    }
}