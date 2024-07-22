using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Installer.Services;
using FortnitePorting.Installer.Views;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Models;
using FortnitePorting.Shared.Models.API.Responses;
using FortnitePorting.Shared.Services;
using FortnitePorting.Shared.Validators;

namespace FortnitePorting.Installer.ViewModels;

public partial class IntroViewModel : ViewModelBase
{
    [ObservableProperty] private string _installationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortnitePorting");
    [ObservableProperty] private ReleaseResponse _releaseInfo;
    [ObservableProperty] private string _releaseVersion;
    [ObservableProperty] private bool _extractOnly;

    public override async Task Initialize()
    {
        var releaseInfo = await ApiVM.FortnitePorting.GetReleaseAsync();
        if (releaseInfo is null)
        {
            TaskService.RunDispatcher(async () =>
            {
                var dialog = new ContentDialog
                {
                    Title = "An Error Occurred",
                    Content = "Failed to retrieve release information. Please contact the developer.",
                    
                    CloseButtonText = "Exit",
                    CloseButtonCommand = new RelayCommand(() =>
                    {
                        ApplicationService.Application.Shutdown();
                    })
                };
                await dialog.ShowAsync();
            });
        }
        else
        {
            ReleaseInfo = releaseInfo;
            ReleaseVersion = ReleaseInfo.Version.GetDisplayString(EVersionStringType.IdentifierPrefix);
        }
    }

    [RelayCommand]
    public async Task BrowseInstallPath()
    {
        if (await BrowseFolderDialog(startLocation: InstallationPath) is { } path)
        {
            InstallationPath = path;
        }
    }

    [RelayCommand]
    public async Task Install()
    {
        AppWM.SetView<InstallView>();
    }
}