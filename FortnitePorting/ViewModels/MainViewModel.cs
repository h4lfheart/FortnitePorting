using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Framework.Controls;
using FortnitePorting.Framework.Services;
using FortnitePorting.Framework.ViewModels.Endpoints.Models;
using FortnitePorting.Services;
using Serilog;

namespace FortnitePorting.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private UserControl activeTab;
    [ObservableProperty] private bool assetTabReady;
    [ObservableProperty] private bool meshTabReady;
    [ObservableProperty] private bool radioTabReady;
    [ObservableProperty] private string updateText;
    [ObservableProperty] private ReleaseResponse? availableUpdate;

    public override async Task Initialize()
    {
        await RefreshUpdateInfo();
        if (AvailableUpdate is not null && !AvailableUpdate.ProperVersion.Equals(Globals.Version))
        {
            UpdateText = $"Update to\nv{AvailableUpdate.Version}";

            if (DateTime.Now >= AppSettings.Current.LastUpdateAskTime.AddDays(1) || !AvailableUpdate.ProperVersion.Equals(AppSettings.Current.LastKnownUpdateVersion))
            {
                AppSettings.Current.LastKnownUpdateVersion = AvailableUpdate.ProperVersion;
                AppSettings.Current.LastUpdateAskTime = DateTime.Now;
                await UpdatePrompt();
            }
        }
        else
        {
            UpdateText = "Check for\nUpdates";
        }
        
        
        ViewModelRegistry.Register<CUE4ParseViewModel>();
        await CUE4ParseVM.Initialize();

        TaskService.Run(async () =>
        {
            await AssetsVM.Initialize();
            AssetTabReady = true;
        });

        TaskService.Run(async () =>
        {
            await FilesVM.Initialize();
            MeshTabReady = true;
        });

        RadioTabReady = true;
    }

    public async Task UpdateCommand()
    {
        await RefreshUpdateInfo();
        await UpdatePrompt();
    }

    public async Task RefreshUpdateInfo()
    {
        AvailableUpdate = await EndpointsVM.FortnitePorting.GetReleaseAsync();
    }

    public async Task UpdatePrompt()
    {
        if (AvailableUpdate is not null && !AvailableUpdate.ProperVersion.Equals(Globals.Version))
        {
            MessageWindow.Show(new MessageWindowModel
            {
                Title = "An Update is Available",
                Text = $"FortnitePorting v{AvailableUpdate.Version} is available. Would you like to update now?",
                Buttons = [
                    new MessageWindowButton("Yes", window =>
                    {
                        window.Close();
                        Update();
                    }),
                    new MessageWindowButton("No", window => window.Close())
                ]
            });
        }
        else
        {
            MessageWindow.Show(new MessageWindowModel
            {
                Title = "No Update Available",
                Text = "FortnitePorting is up-to-date."
            });
        }
    }

    private void Update()
    {
        TaskService.Run(() =>
        {
            EndpointsVM.DownloadFile(AvailableUpdate.DownloadUrl, "FortnitePorting.temp.exe");
            Process.Start(new ProcessStartInfo
            {
                FileName = DependencyService.UpdaterFile.FullName,
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            });
            Shutdown();
        });
    }
}