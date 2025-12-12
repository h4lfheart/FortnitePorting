using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Models;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Views;

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

    [ObservableProperty] private OnlineResponse? _onlineStatus;

    private const string PORTLE_URL = "https://portle.halfheart.dev/release/Portle.exe";

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

        await CheckForUpdate(isAutomatic: true);
    }

    public async Task CheckForUpdate(bool isAutomatic = false)
    {
        void NoUpdate()
        {
            if (isAutomatic) return;

            TaskService.RunDispatcher(async () =>
            {
                var dialog = new ContentDialog
                {
                    Title = "No Update Available",
                    Content = "Fortnite Porting is up to date.",
                    CloseButtonText = "Continue"
                };

                await dialog.ShowAsync();
            });
        }

        var repositoryInfo = await Api.FortnitePorting.Repository();
        if (repositoryInfo is not null)
        {
            var newestRelease = repositoryInfo.Versions.MaxBy(version => version.UploadTime)!;
            if (newestRelease.Version <= Globals.Version)
            {
                NoUpdate();
                return;
            }

            if (isAutomatic && Settings.Application.LastOnlineVersion == newestRelease.Version)
            {
                return;
            }

            Settings.Application.LastOnlineVersion = newestRelease.Version;

            await TaskService.RunDispatcherAsync(async () =>
            {
                var dialog = new ContentDialog
                {
                    Title = "Update Available",
                    Content =
                        $"Fortnite Porting {newestRelease.Version.GetDisplayString(EVersionStringType.IdentifierPrefix)} is now available. Would you like to update?",
                    CloseButtonText = "No",
                    PrimaryButtonText = "Yes",
                    PrimaryButtonCommand = new RelayCommand(async () =>
                    {
                        if (!File.Exists(Settings.Application.PortlePath) ||
                            (!Settings.Application.UsePortlePath && (!Api.GetHash(PORTLE_URL)
                                ?.Equals(Settings.Application.PortlePath.GetHash()) ?? false)))
                        {
                            await Api.DownloadFileAsync(PORTLE_URL, Settings.Application.PortlePath);
                        }

                        var args = new[]
                        {
                            "--silent",
                            "--skip-setup",
                            $"--add-repository https://api.fortniteporting.app/v1/static/repository",
                            $"--import-profile \"Fortnite Porting\" \"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppDomain.CurrentDomain.FriendlyName + ".exe")}\" \"FortnitePorting\"",
                            "--update-profile \"Fortnite Porting\" -force",
                            "--launch-profile \"Fortnite Porting\"",
                        };

                        Process.Start(new ProcessStartInfo
                        {
                            FileName = Settings.Application.PortlePath,
                            Arguments = string.Join(' ', args),
                            WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                            UseShellExecute = true
                        });

                        App.Lifetime.Shutdown();
                    })
                };

                await dialog.ShowAsync();
            });

            return;
        }

        NoUpdate();
    }

}