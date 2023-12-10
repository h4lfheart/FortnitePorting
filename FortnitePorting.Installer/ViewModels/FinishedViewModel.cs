using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;
using FortnitePorting.Framework.Services;
using Serilog;

namespace FortnitePorting.Installer.ViewModels;

public partial class FinishedViewModel : ViewModelBase
{
    [ObservableProperty] private bool launchAfterExit = true;

    public async Task Continue()
    {
        await TaskService.RunAsync(() =>
        {
            if (LaunchAfterExit)
            {
                Process.Start(Path.Combine(MainVM.InstallationPath, "FortnitePorting.exe"));
            }
        
            Shutdown();
        });
       
    }
}