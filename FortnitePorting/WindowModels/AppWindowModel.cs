using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ATL.Logging;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Models;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.App;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Models;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;
using FortnitePorting.ViewModels.Settings;
using FortnitePorting.Views;
using InfoBarData = FortnitePorting.Models.App.InfoBarData;
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
    [ObservableProperty] private int _chatNotifications;

    [ObservableProperty] private bool _timeWasterOpen;
    [ObservableProperty] private TimeWasterView? _timeWaster;

    [ObservableProperty] private OnlineResponse? _onlineStatus;
    
    public OnlineSettingsViewModel OnlineRef => AppSettings.Current.Online;
    
    public override async Task Initialize()
    {
        SetupTabsAreVisible = !AppSettings.Current.Installation.FinishedWelcomeScreen;

        OnlineStatus = await ApiVM.FortnitePorting.GetOnlineStatusAsync();

        await CheckForUpdate(isAutomatic: true);
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
    
    public void Message(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational, bool autoClose = true, string id = "", float closeTime = 3f)
    {
        Message(new InfoBarData(title, message, severity, autoClose, id, closeTime));
    }

    public void Message(InfoBarData data)
    {
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