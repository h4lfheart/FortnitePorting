using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Information;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Views;
using Serilog;

namespace FortnitePorting.WindowModels;

public partial class AppWindowModel(
    InfoService info,
    SettingsService settings,
    SupabaseService supabase,
    CUE4ParseService cue4Parse,
    BlackHoleService blackHole) : WindowModelBase
{
    [ObservableProperty] private InfoService _info = info;
    [ObservableProperty] private SettingsService _settings = settings;
    [ObservableProperty] private SupabaseService _supaBase = supabase;
    [ObservableProperty] private CUE4ParseService _UEParse = cue4Parse;
    [ObservableProperty] private BlackHoleService _blackHole = blackHole;
    
    [ObservableProperty] private string _versionString = Globals.VersionString;

    [ObservableProperty] private int _chatNotifications;
    [ObservableProperty] private int _unsubmittedPolls;

    [ObservableProperty] private SetupView? _setupViewContent;
    
    [ObservableProperty] private RepositoryVersion? _updateVersion;

    [ObservableProperty] private OnlineResponse? _onlineStatus;
    [ObservableProperty] private BroadcastResponse[] _broadcasts = [];

    private const string PORTLE_URL = "https://cdn.fortniteporting.app/portle/Portle.exe";

    public override async Task Initialize()
    {
        if (!AppSettings.Installation.FinishedSetup)
        {
            await TaskService.RunDispatcherAsync(() =>
            {
                SetupViewContent = new SetupView();
            });
        }
        
        OnlineStatus = await Api.FortnitePorting.Online();

        foreach (var broadcast in await Api.FortnitePorting.Broadcasts())
        {
            Info.Broadcast(broadcast);
        }

        await CheckForUpdate();
    }

    [RelayCommand]
    public async Task Update()
    {
        var remoteHash = Api.GetHash(PORTLE_URL) ?? string.Empty;
        var localHash = Settings.Application.PortlePath.GetHash();
        
        if (!File.Exists(Settings.Application.PortlePath) || (!Settings.Application.UsePortlePath && !remoteHash.Equals(localHash, StringComparison.OrdinalIgnoreCase)))
        {
            Log.Information($"Updating portle executable from {PORTLE_URL} at {Settings.Application.PortlePath}");
            await Api.DownloadFileAsync(PORTLE_URL, Settings.Application.PortlePath);
        }

        var args = new[]
        {
            "--skip-setup",
            "--add-repository https://api.fortniteporting.app/v1/static/repository",
            $"--import-profile \"Fortnite Porting\" \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppDomain.CurrentDomain.FriendlyName + ".exe")}\" \"FortnitePorting\"",
            "--update-profile \"Fortnite Porting\" -force",
            "--launch-profile \"Fortnite Porting\"",
        };
        
        Info.Message("Portle", $"Fortnite Porting {UpdateVersion!.Version} is currently being downloaded.");

        await Task.Delay(2500);
        
        Process.Start(new ProcessStartInfo
        {
            FileName = Settings.Application.PortlePath,
            Arguments = string.Join(' ', args),
            WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
            UseShellExecute = true
        });
        
        App.Lifetime.Shutdown();
    }

    public async Task CheckForUpdate()
    {
        var repositoryInfo = await Api.FortnitePorting.Repository();
        var newestVersion = repositoryInfo?.Versions.MaxBy(version => version.UploadTime);
        if (newestVersion is null || newestVersion.Version <= Globals.Version) return;
        
        UpdateVersion = newestVersion;
    }

}