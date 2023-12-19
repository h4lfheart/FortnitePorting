using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
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
    [ObservableProperty] private bool updateAvailable;
    [ObservableProperty] private string updateText;
    [ObservableProperty] private ReleaseResponse availableUpdate;

    public override async Task Initialize()
    {
        AvailableUpdate = await EndpointsVM.FortnitePorting.GetReleaseAsync();
        if (AvailableUpdate is not null && !AvailableUpdate.ProperVersion.Equals(Globals.Version))
        {
            UpdateAvailable = true;
            UpdateText = $"Update to\nv{AvailableUpdate.Version}";
            PromptForUpdate();
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

    public void PromptForUpdate()
    {
        MessageWindow.Show(new MessageWindowModel
        {
            Title = "An Update is Available",
            Text = $"The latest version of FortnitePorting is v{AvailableUpdate.Version} and you are using v{Globals.VersionString}. Do you want to update now?",
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

    private void Update()
    {
        TaskService.Run(() =>
        {
            EndpointsVM.DownloadFile(AvailableUpdate.DownloadUrl, "FortnitePorting.TEMP.exe");
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