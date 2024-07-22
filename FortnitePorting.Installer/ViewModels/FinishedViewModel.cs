using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Installer.Services;
using FortnitePorting.Installer.Views;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Models;
using FortnitePorting.Shared.Services;
using FortnitePorting.Shared.Validators;

namespace FortnitePorting.Installer.ViewModels;

public partial class FinishedViewModel : ViewModelBase
{
    [ObservableProperty] private bool _launchOnExit = true;
    [ObservableProperty] private bool _createDesktopShortcut = true;
    
    [RelayCommand]
    public async Task Finish()
    {
        var executablePath = Path.Combine(IntroVM.InstallationPath, "FortnitePorting.exe");
        
        if (CreateDesktopShortcut)
        {
            var desktopDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            
            await using var writer = new StreamWriter(Path.Combine(desktopDirectory, "FortnitePorting.url"));
            await writer.WriteLineAsync("[InternetShortcut]");
            await writer.WriteLineAsync($"URL=file:///{executablePath}");
            await writer.WriteLineAsync("IconIndex=0");
            await writer.WriteLineAsync($"IconFile={executablePath}");
        }

        if (LaunchOnExit)
        {
            Launch(executablePath);
        }

        ApplicationService.Application.Shutdown();
    }
}