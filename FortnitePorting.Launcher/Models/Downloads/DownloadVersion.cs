using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Launcher.Application;
using FortnitePorting.Launcher.Extensions;
using FortnitePorting.Launcher.Models.API.Response;
using FortnitePorting.Launcher.Models.Installation;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Models;
using Newtonsoft.Json;

namespace FortnitePorting.Launcher.Models.Downloads;

public partial class DownloadVersion : ObservableObject
{
    [ObservableProperty] private DownloadRepository _parentRepository;
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(DisplayString))] private FPVersion _version;
    [ObservableProperty] private string _executableUrl;
    [ObservableProperty] private DateTime _uploadTime;
    [ObservableProperty] private bool _isCurrentlyDownloading;

    [ObservableProperty, JsonIgnore] private float _downloadProgressFraction;

    public string DisplayString => $"{ParentRepository.Title} {Version}";

    public bool IsDownloaded => File.Exists(ExecutableDownloadPath) && !IsCurrentlyDownloading;

    public string ExecutableDownloadPath => Path.Combine(AppSettings.Current.DownloadsPath, ParentRepository.Title, Version.ToString(), ExecutableUrl.SubstringAfterLast("/"));

    public InstallationVersion CreateInstallationVersion()
    {
        return new InstallationVersion
        {
            RepositoryName = ParentRepository.Title,
            Version = Version,
            ExecutablePath = ExecutableDownloadPath,
            RepositoryUrl = ParentRepository.RepositoryUrl,
            IconUrl = ParentRepository.IconUrl
        };
    }

    
    [RelayCommand]
    public async Task Download()
    {
        await DownloadInstallationVersion();
    }
    
    public async Task<InstallationVersion> DownloadInstallationVersion()
    {
        if (IsDownloaded)
        {
            return CreateInstallationVersion();
        }
        
        IsCurrentlyDownloading = true;
        var downloadedFile = await ApiVM.DownloadFileAsync(ExecutableUrl, ExecutableDownloadPath, progress => DownloadProgressFraction = progress);
        IsCurrentlyDownloading = false;
        DownloadProgressFraction = 0;
        
        if (!downloadedFile.Exists)
        {
            AppWM.Message("Downloads", $"Failed to download {ExecutableUrl}", InfoBarSeverity.Error);
            return null;
        }
        
        OnPropertyChanged(nameof(IsDownloaded));

        var installationVersion = CreateInstallationVersion();

        AppSettings.Current.DownloadedVersions.Add(installationVersion);

        return installationVersion;
    }
    
    [RelayCommand]
    public async Task Delete()
    {
        var profilesUsingVersion = ProfilesVM.ProfilesSource.Items
            .Where(profile => profile.Version.Equals(Version))
            .ToArray();

        var cancelledDeletion = false;
        if (profilesUsingVersion.Length > 0)
        {
            var dialog = new ContentDialog
            {
                Title = $"Delete \"{DisplayString}\"",
                Content =
                    $"Are you sure you would like to delete this version? There {(profilesUsingVersion.Length == 1 ? $"is {profilesUsingVersion.Length} profile that relies" : $"are {profilesUsingVersion.Length} profiles that rely")} on this version.",
                PrimaryButtonText = "Delete",
                PrimaryButtonCommand = new RelayCommand(async () =>
                {
                    // remove associated profiles
                    foreach (var profile in profilesUsingVersion)
                    {
                        await profile.DeleteAndCleanup();
                        ProfilesVM.ProfilesSource.Remove(profile);
                    }
                }),
                CloseButtonText = "Cancel",
                CloseButtonCommand = new RelayCommand(() => cancelledDeletion = true)
            };

            await dialog.ShowAsync();
        }
        
        if (cancelledDeletion) return;
        
        File.Delete(ExecutableDownloadPath);
        Directory.Delete(Path.Combine(AppSettings.Current.DownloadsPath, ParentRepository.Title, Version.ToString()));

        AppSettings.Current.DownloadedVersions.RemoveAll(version => version.ExecutablePath == ExecutableDownloadPath);
        
        OnPropertyChanged(nameof(IsDownloaded));
    }
}