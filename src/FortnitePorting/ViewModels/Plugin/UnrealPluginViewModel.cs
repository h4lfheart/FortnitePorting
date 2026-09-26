using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Models.Plugin;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.ViewModels.Plugin;

public partial class UnrealPluginViewModel : PluginInstallationViewModelBase<UnrealInstallation>
{
    protected override DirectoryInfo PluginWorkingDirectory => UnrealInstallation.PluginWorkingDirectory;

    protected override bool TrySyncVersion(UnrealInstallation installation) => installation.SyncVersion();

    protected override void Uninstall(UnrealInstallation installation) => installation.Uninstall();

    public override async Task AddInstallation()
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

    public override async Task SyncInstallations(bool verbose)
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
