using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;
using FortnitePorting.Installer.Views;

namespace FortnitePorting.Installer.ViewModels;

public partial class InstallViewModel : ViewModelBase
{
    [ObservableProperty] private string statusTitleText = "Installing";
    [ObservableProperty] private string subTitleText = "Starting Installation Process";

    private static readonly DirectoryInfo TempDirectory = new DirectoryInfo(Path.GetTempPath());
    
    public override async Task Initialize()
    {
        var response = await EndpointsVM.FortnitePorting.GetReleaseAsync();
        if (response is null) return; // todo need message box in framework proj

        foreach (var dependency in response.Dependencies)
        {
            StatusTitleText = $"Installing: {dependency.Name}";
            SubTitleText = $"Downloading {dependency.URL}";
            var downloadedFile = await EndpointsVM.DownloadFileAsync(dependency.URL, TempDirectory);
            
            SubTitleText = $"Running {downloadedFile.Name}";
            var dependencyProcess = Process.Start(new ProcessStartInfo
            {
                FileName = downloadedFile.FullName,
                Arguments = "/install /quiet /norestart",
                UseShellExecute = true
            });
            await dependencyProcess!.WaitForExitAsync();
            
            downloadedFile.Delete();
        }
        
        StatusTitleText = $"Installing: FortnitePorting v{response.Version}";
        SubTitleText = $"Downloading {response.DownloadUrl}";

        var installationDirectory = new DirectoryInfo(MainVM.InstallationPath);
        installationDirectory.Create();
        
        await EndpointsVM.DownloadFileAsync(response.DownloadUrl, installationDirectory);
        
        AppVM.SetView<FinishedView>();
    }
}