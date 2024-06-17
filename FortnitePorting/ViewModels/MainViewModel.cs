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
using FortnitePorting.Services;
using FortnitePorting.ViewModels.Endpoints.Models;
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
        if (AppSettings.Current.FirstStartup)
        {
            MessageWindow.Show("Data Collection",
                "Would you like to opt-in to data collection for Fortnite Porting V2?\n\nThe only data collected will be your installation's UUID and Version and will be used for tracking the amount of daily users on V2.\n\nNo personal data is sent and it is entirely anonymous and optional.",
                [
                    new MessageWindowButton("Yes", ctx =>
                    {
                        AppSettings.Current.AllowDataCollection = true;
                        TaskService.Run(EndpointsVM.FortnitePorting.PostStatsAsync);
                        ctx.Close();
                    }),
                    new MessageWindowButton("No", ctx => ctx.Close())
                ]);

            AppSettings.Current.FirstStartup = false;
        }

        if (AppSettings.Current.AllowDataCollection)
        {
            TaskService.Run(EndpointsVM.FortnitePorting.PostStatsAsync);
        }
        
        
        await RefreshUpdateInfo();
        if (AvailableUpdate is not null && AvailableUpdate.ProperVersion > Globals.Version)
        {
            UpdateText = $"Update to\nv{AvailableUpdate.Version}";

            if (DateTime.Now >= AppSettings.Current.LastUpdateAskTime.AddDays(0.5) && !AvailableUpdate.ProperVersion.Equals(AppSettings.Current.LastKnownUpdateVersion))
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
        if (AvailableUpdate is not null && AvailableUpdate.ProperVersion > Globals.Version)
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
        AppSettings.Save();
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