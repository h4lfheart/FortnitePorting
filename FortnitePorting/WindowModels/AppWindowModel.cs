using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ATL.Logging;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Models;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.App;
using FortnitePorting.Models.OG;
using FortnitePorting.Models.TimeWaster.Audio;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Models;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;
using FortnitePorting.ViewModels.Settings;
using FortnitePorting.Views;
using LibVLCSharp.Shared;
using InfoBarData = FortnitePorting.Shared.Models.App.InfoBarData;
using Log = Serilog.Log;

namespace FortnitePorting.WindowModels;

public partial class AppWindowModel : WindowModelBase
{
    [ObservableProperty] private string _versionString = Globals.VersionString;
    [ObservableProperty] private bool _gameBasedTabsAreReady = false;
    [ObservableProperty] private bool _setupTabsAreVisible = true;
    [ObservableProperty] private bool _onlineAndGameTabsAreVisible = false;
    [ObservableProperty] private Frame _contentFrame;
    [ObservableProperty] private NavigationView _navigationView;
    [ObservableProperty] private ObservableCollection<InfoBarData> _infoBars = [];
    [ObservableProperty] private TitleData? _titleData;

    [ObservableProperty] private MediaPlayer _mediaPlayer;
    [ObservableProperty] private bool _splashOpen = true;
    [ObservableProperty] private bool _closingSplash;
    [ObservableProperty] private float _fadeOutBlackOpacity = 0.0f;
    [ObservableProperty] private float _fadeInBackOpacity = 1.0f;
    
    [ObservableProperty] private int _chatNotifications;
    [ObservableProperty] private int _unsubmittedPolls;

    [ObservableProperty] private bool _timeWasterOpen;
    [ObservableProperty] private TimeWasterView? _timeWaster;

    [ObservableProperty] private OnlineResponse? _onlineStatus;

    [ObservableProperty] private Rect _bounds;
    [ObservableProperty] private SupplyDrop _supplyDrop = new();
    
    public OnlineSettingsViewModel OnlineRef => AppSettings.Current.Online;
    
    private readonly LibVLC _vlc = new("--input-repeat=2");


    public override async Task Initialize()
    {
        MediaPlayer = new MediaPlayer(_vlc)
        {
            Volume = 75,
            Scale = 1
        };

        SetupTabsAreVisible = !AppSettings.Current.Installation.FinishedWelcomeScreen;

        OnlineStatus = await ApiVM.FortnitePorting.GetOnlineStatusAsync();

        await CheckForUpdate(isAutomatic: true);
    }

    public void PlayOGSplash()
    {
        SplashOpen = true;
        
        using var media = new Media(_vlc, new Uri("https://fortniteporting.halfheart.dev/OG/FPOGSplash.mp4"));
        MediaPlayer.Play(media);
    }
    
    // idc about code quality for a 1 day release
    
    public void StopOGSplash()
    {
        if (ClosingSplash) return;

        ClosingSplash = true;
        
        var totalTime = 0.0f;
        var fadeTime = 1.0f;
        var deltaTime = 1f / 60f;
        
        var fadeInTimer = new DispatcherTimer(DispatcherPriority.Background);
        fadeInTimer.Interval = TimeSpan.FromSeconds(deltaTime);
        fadeInTimer.Tick += (sender, args) =>
        {
            if (totalTime >= fadeTime)
            {
                fadeInTimer.Stop();
                return;
            }
            
            FadeInBackOpacity = float.Lerp(1, 0, totalTime / fadeTime);

            totalTime += deltaTime;
        };
        
        var fadeOutTimer = new DispatcherTimer(DispatcherPriority.Background);
        fadeOutTimer.Interval = TimeSpan.FromSeconds(deltaTime);
        fadeOutTimer.Tick += (sender, args) =>
        {
            if (totalTime >= fadeTime)
            {
                // idk why but this stops it from crashing
                var oldPlayer = MediaPlayer;
                MediaPlayer.Stop();
                MediaPlayer = null;
                oldPlayer.Dispose();
                
                SplashOpen = false;

                totalTime = 0;
                fadeInTimer.Start();
                fadeOutTimer.Stop();
                return;
            }

            MediaPlayer.Volume = (int)float.Lerp(75, 0, totalTime / fadeTime);
            FadeOutBlackOpacity = totalTime / fadeTime;

            totalTime += deltaTime;
        };
        
        fadeOutTimer.Start();
    }

    
    public override async Task OnViewOpened()
    {
        if (Design.IsDesignMode) return;

        var isWaitingForSpawn = false;
        DispatcherTimer.Run(() =>
        {
            if (!AppSettings.Current.Application.UseSupplyDrops)
            {
                if (SupplyDrop.IsAlive)
                {
                    SupplyDrop.Destroy();
                    AudioSystem.Instance.Stop();
                }
                return true;
            }

            if (MediaPlayer.IsPlaying) return true;
            
            if (!CUE4ParseVM.FinishedLoading) return true;
            if (TimeWasterOpen) return true;
            if (isWaitingForSpawn) return true;
            if (SupplyDrop.IsOpening) return true;
            
            if (SupplyDrop.IsAlive)
            {
                SupplyDrop.Update();

                if (SupplyDrop.YPosition > Bounds.Height + 100)
                {
                    SupplyDrop.Destroy();
                }
            }
            else
            {
                isWaitingForSpawn = true;
                TaskService.Run(async () =>
                {
                    var time = Random.Shared.Next(60_000, 240_000);
                    await Task.Delay(time);

                    SupplyDrop.Spawn();
                    isWaitingForSpawn = false;
                });
            }

            return true;
        }, TimeSpan.FromSeconds(SupplyDrop.DELTA_TIME), DispatcherPriority.Background);
    }

    public async Task CheckForUpdate(bool isAutomatic = false)
    {
        var releaseInfo = await ApiVM.FortnitePorting.GetReleaseAsync();
        if (releaseInfo is not null && releaseInfo.Version > Globals.Version)
        {
            if (isAutomatic && AppSettings.Current.Application.LastOnlineVersion == releaseInfo.Version) return;

            AppSettings.Current.Application.LastOnlineVersion = releaseInfo.Version;
        
            await TaskService.RunDispatcherAsync(async () =>
            {
                var dialog = new ContentDialog
                {
                    Title = "Update Available",
                    Content = $"Fortnite Porting {releaseInfo.Version.GetDisplayString(EVersionStringType.IdentifierPrefix)} is now available. Would you like to update?",
                    CloseButtonText = "No",
                    PrimaryButtonText = "Yes",
                    PrimaryButtonCommand = new RelayCommand(async () =>
                    {
                        var updatedExecutableName = $"FortnitePorting-{releaseInfo.Version}.exe";
                        var updatedPath = Path.Combine(DataFolder.FullName, "release", updatedExecutableName);
                        if (!File.Exists(updatedPath))
                        {
                            Message("Updating", $"Downloading {updatedExecutableName}", autoClose: false);
                            await ApiVM.DownloadFileAsync(releaseInfo.Download, updatedPath);
                        }

                        var applicationPath = Process.GetCurrentProcess().MainModule!.FileName;
                        
                        using var updateProcess = new Process();
                        updateProcess.StartInfo = new ProcessStartInfo
                        {
                            FileName = DependencyService.UpdaterFile.FullName,
                            Arguments = $"\"{updatedPath}\" \"{applicationPath}\"",
                            WorkingDirectory = Path.GetDirectoryName(applicationPath),
                            UseShellExecute = true
                        };

                        updateProcess.Start();
                        
                        ApplicationService.Application.Shutdown();
                    })
                };

                await dialog.ShowAsync();
            });
        }
        else
        {
            if (isAutomatic) return;
            
            await TaskService.RunDispatcherAsync(async () =>
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
        
    }
    
    public void Message(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational, bool autoClose = true, string id = "", float closeTime = 3f, bool useButton = false, string buttonTitle = "", Action? buttonCommand = null)
    {
        Message(new InfoBarData(title, message, severity, autoClose, id, closeTime, useButton, buttonTitle, buttonCommand));
    }

    public void Message(InfoBarData data)
    {
        if (!string.IsNullOrEmpty(data.Id))
            InfoBars.RemoveAll(bar => bar.Id.Equals(data.Id));
        
        InfoBars.Add(data);
        if (!data.AutoClose) return;
        
        TaskService.Run(async () =>
        {
            await Task.Delay((int) (data.CloseTime * 1000));
            InfoBars.Remove(data);
        });
    }
    
    public void UpdateMessage(string id, string message)
    {
        var foundInfoBar = InfoBars.FirstOrDefault(infoBar => infoBar.Id == id);
        if (foundInfoBar is null) return;
        
        foundInfoBar.Message = message;
    }
    
    public void CloseMessage(string id)
    {
        InfoBars.RemoveAll(info => info.Id == id);
    }
    
    public void Dialog(string title, string content)
    {
        TaskService.RunDispatcher(async () =>
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "Continue"
            };
            
            await dialog.ShowAsync();
        });
    }

    public void Title(string title, string subTitle, float time = 5.0f)
    {
        TitleData = new TitleData(title, subTitle);
        TaskService.Run(async () =>
        {
            await Task.Delay((int) (time * 1000));
            TitleData = null;
        });
    }
    

    public void Navigate<T>()
    {
        Navigate(typeof(T));
    }
    
    public void Navigate(Type type)
    {
        ContentFrame.Navigate(type, null, AppSettings.Current.Application.Transition);

        var buttonName = type.Name.Replace("View", string.Empty);
        NavigationView.SelectedItem = NavigationView.MenuItems
            .Concat(NavigationView.FooterMenuItems)
            .OfType<NavigationViewItem>()
            .FirstOrDefault(item => (string) item.Tag! == buttonName);
    }

    public bool IsInView<T>()
    {
        var result = false;
        TaskService.RunDispatcher(() =>
        {
            result = AppWM.ContentFrame.CurrentSourcePageType == typeof(T);
        });
        return result;
    }

    public void ToggleVisibility(bool vis)
    {
        ContentFrame.IsVisible = vis;
    }
}