using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
    CUE4ParseService ueParse,
    BlackHoleService blackHole,
    ChatService chat,
    APIService api,
    AppService app) : WindowModelBase
{
    [ObservableProperty] private InfoService _info = info;
    [ObservableProperty] private SettingsService _settings = settings;
    [ObservableProperty] private SupabaseService _supaBase = supabase;
    [ObservableProperty] private CUE4ParseService _UEParse = ueParse;
    [ObservableProperty] private BlackHoleService _blackHole = blackHole;
    [ObservableProperty] private ChatService _chat = chat;

    private readonly APIService _api = api;
    private readonly AppService _app = app;

    [ObservableProperty] private string _versionString = Globals.Version.Identifier switch
    {
        "dev" => "dev-build",
        var hash when CommitShaMatch().IsMatch(hash) => hash,
        _ => Globals.VersionString
    };
    [ObservableProperty] private int _unreadNewsCount;
    [ObservableProperty] private int _chatNotifications;
    [ObservableProperty] private int _unsubmittedPolls;
    [ObservableProperty] private SetupView? _setupViewContent;
    [ObservableProperty] private RepositoryVersion? _updateVersion;
    [ObservableProperty] private BroadcastResponse[] _broadcasts = [];

    private const string PORTLE_URL = "https://cdn.fortniteporting.app/portle/Portle.exe";

    public override async Task Initialize()
    {
        if (!_settings.Installation.FinishedSetup)
        {
            await TaskService.RunDispatcherAsync(() =>
            {
                SetupViewContent = new SetupView();
            });
        }

        var broadcastResponse = await _api.FortnitePorting.Broadcasts();
        foreach (var broadcast in broadcastResponse.Entries)
        {
            if (!broadcast.IsEnabled)
                continue;

            var satisfiesMaxVersion = broadcast.MaxVersion is null || Globals.Version <= broadcast.MaxVersion;
            var satisfiesMinVersion = broadcast.MinVersion is null || Globals.Version >= broadcast.MinVersion;

            if (satisfiesMaxVersion && satisfiesMinVersion)
                _info.Broadcast(broadcast);
        }

        await CheckForUpdate();
    }

    [RelayCommand]
    public async Task Update()
    {
        var remoteHash = _api.GetHash(PORTLE_URL) ?? string.Empty;

        if (!File.Exists(_settings.Developer.PortlePath) || (!_settings.Developer.UsePortlePath && !remoteHash.Equals(_settings.Developer.PortlePath.GetHash(), StringComparison.OrdinalIgnoreCase)))
        {
            Log.Information($"Updating portle executable from {PORTLE_URL} at {_settings.Developer.PortlePath}");
            await _api.DownloadFileAsync(PORTLE_URL, _settings.Developer.PortlePath);
        }

        var args = new[]
        {
            "--skip-setup",
            "--add-repository https://api.fortniteporting.app/v1/repository",
            $"--import-profile \"Fortnite Porting\" \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppDomain.CurrentDomain.FriendlyName + ".exe")}\" \"FortnitePorting\"",
            "--update-profile \"Fortnite Porting\" -force",
            "--launch-profile \"Fortnite Porting\"",
        };

        _info.Message("Portle", $"Fortnite Porting {UpdateVersion!.Version} is currently being downloaded.");

        await Task.Delay(2500);

        Process.Start(new ProcessStartInfo
        {
            FileName = _settings.Developer.PortlePath,
            Arguments = string.Join(' ', args),
            WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
            UseShellExecute = true
        });

        _app.Shutdown();
    }

    public async Task CheckForUpdate()
    {
        if (Globals.IsDevBuild) return;

        var repositoryInfo = await _api.FortnitePorting.Repository();
        var newestVersion = repositoryInfo?.Versions.MaxBy(version => version.UploadTime);
        if (newestVersion is null || newestVersion.Version <= Globals.Version) return;

        UpdateVersion = newestVersion;

        if (DateTime.Today > newestVersion.UploadTime.AddDays(6))
        {
            var outOfDateDays = DateTime.Today - newestVersion.UploadTime;
            _info.Dialog($"Update {newestVersion.Version}", $"Your Fortnite Porting is {outOfDateDays.Days} days out of date, please consider updating.", buttons: [
                new DialogButton
                {
                    Text = "Update",
                    Action = () => TaskService.Run(Update)
                }
            ]);
        }
    }

    [GeneratedRegex(@"^[0-9a-f]{7}$")]
    private static partial Regex CommitShaMatch();
}