using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;
using FortnitePorting.Services;

namespace FortnitePorting.ViewModels.Plugin;

public abstract partial class PluginInstallationViewModelBase<TInstallation> : ViewModelBase
    where TInstallation : class
{
    [ObservableProperty] private bool _automaticallySync = true;
    [ObservableProperty] private ObservableCollection<TInstallation> _installations = [];

    protected abstract DirectoryInfo PluginWorkingDirectory { get; }

    protected abstract bool TrySyncVersion(TInstallation installation);
    protected abstract void Uninstall(TInstallation installation);

    public override async Task Initialize()
    {
        if (!PluginWorkingDirectory.Exists)
            PluginWorkingDirectory.Create();

        foreach (var installation in Installations.ToArray())
        {
            if (TrySyncVersion(installation)) continue;

            Uninstall(installation);
            Installations.Remove(installation);
        }
    }

    public async Task RemoveInstallation(TInstallation installation)
    {
        TaskService.Run(() =>
        {
            Uninstall(installation);
            Installations.Remove(installation);
        });
    }

    public async Task SyncInstallations()
    {
        await SyncInstallations(true);
    }

    public abstract Task AddInstallation();
    public abstract Task SyncInstallations(bool verbose);
}
