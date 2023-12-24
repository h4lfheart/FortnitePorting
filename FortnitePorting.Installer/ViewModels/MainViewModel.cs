using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;
using FortnitePorting.Installer.Views;

namespace FortnitePorting.Installer.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private string installationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortnitePorting");

    public async Task BrowseInstallPath()
    {
        if (await BrowseFolderDialog(startLocation: InstallationPath) is { } path)
        {
            InstallationPath = path;
        }
    }

    public void StartInstall()
    {
        AppVM.SetView<InstallView>();
    }
}