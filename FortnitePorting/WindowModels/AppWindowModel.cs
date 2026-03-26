using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Extensions;
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

    [ObservableProperty] private double _chippyOpacity = 1.0d;
    [ObservableProperty] private string _chippyText = "nice to meet you!!";
    [ObservableProperty] private Bitmap _chippyImage = _chippyIdle;

    private string[] _textSet = [];

    private static readonly Bitmap _chippyIdle =
        ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/ApricotFudge/chippy_idle.png");
    
    private static readonly Bitmap _chippyTada =
        ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/ApricotFudge/chippy_tada.png");
    
    private static readonly Bitmap _chippyExplain =
        ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/ApricotFudge/chippy_explain.png");
    
    private static readonly Bitmap _chippyError =
        ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/ApricotFudge/chippy_error.png");

    private static readonly Bitmap[] _chippyTalkImages =
    [
        _chippyIdle,
        _chippyTada,
        _chippyExplain
    ];

    private const string PORTLE_URL = "https://cdn.fortniteporting.app/portle/Portle.exe";

    public override async Task Initialize()
    {
        var timer = new DispatcherTimer(TimeSpan.FromSeconds(5), DispatcherPriority.Normal,
            (sender, args) =>
            {
                TickChippy();
            });
        
        timer.Start();
        
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

    public void UpdateChippy(string[] textSet)
    {
        _textSet = textSet;
        TickChippy();
    }

    private void TickChippy()
    {
        ChippyText = _textSet.Random()!;
        if (_textSet.Length > 1)
            ChippyImage = _chippyTalkImages.Random()!;
    }

    
    [RelayCommand]
    public async Task Update()
    {
        var remoteHash = Api.GetHash(PORTLE_URL) ?? string.Empty;
        
        if (!File.Exists(Settings.Developer.PortlePath) || (!Settings.Developer.UsePortlePath && !remoteHash.Equals(Settings.Developer.PortlePath.GetHash(), StringComparison.OrdinalIgnoreCase)))
        {
            Log.Information($"Updating portle executable from {PORTLE_URL} at {Settings.Developer.PortlePath}");
            await Api.DownloadFileAsync(PORTLE_URL, Settings.Developer.PortlePath);
        }

        var args = new[]
        {
            "--skip-setup",
            "--add-repository https://api.fortniteporting.app/v1/repository",
            $"--import-profile \"Fortnite Porting\" \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppDomain.CurrentDomain.FriendlyName + ".exe")}\" \"FortnitePorting\"",
            "--update-profile \"Fortnite Porting\" -force",
            "--launch-profile \"Fortnite Porting\"",
        };
        
        Info.Message("Portle", $"Fortnite Porting {UpdateVersion!.Version} is currently being downloaded.");

        await Task.Delay(2500);
        
        Process.Start(new ProcessStartInfo
        {
            FileName = Settings.Developer.PortlePath,
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

        if (DateTime.Today > newestVersion.UploadTime.AddDays(6))
        {
            var outOfDateDays = DateTime.Today - newestVersion.UploadTime;
            Info.Dialog($"Update {newestVersion.Version}", $"Your Fortnite Porting is {outOfDateDays.Days} days out of date, please consider updating.", buttons: [
                new DialogButton
                {
                    Text = "Update",
                    Action = () => TaskService.Run(Update)
                }
            ]);
        }
    }

}