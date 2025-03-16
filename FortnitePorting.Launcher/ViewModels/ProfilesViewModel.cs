using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Launcher.Application;
using FortnitePorting.Launcher.Models.Downloads;
using FortnitePorting.Launcher.Models.Installation;
using FortnitePorting.Launcher.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using ReactiveUI;

namespace FortnitePorting.Launcher.ViewModels;

public partial class ProfilesViewModel : ViewModelBase
{
    [ObservableProperty] private string _searchFilter = string.Empty;
    
    [ObservableProperty] private ReadOnlyObservableCollection<InstallationProfile> _profiles = new([]);
    
    [JsonIgnore] public bool CanCreateProfile => AppSettings.Current.DownloadedVersions.Count > 0 || AppSettings.Current.Repositories.Count > 0;
    
    public readonly SourceList<InstallationProfile> ProfilesSource = new();

    public ProfilesViewModel()
    {
        var filter = this
            .WhenAnyValue(viewModel => viewModel.SearchFilter)
            .Select(searchFilter =>
            {
                return new Func<InstallationProfile, bool>(profile => 
                    MiscExtensions.Filter(profile.Name, searchFilter) || MiscExtensions.Filter(profile.Version.ToString(), searchFilter));
            });
        
        ProfilesSource.Connect()
            .Filter(filter)
            .Bind(out var collection)
            .Subscribe();
        
        Profiles = collection;
    }

    public async Task UpdateRepositoryProfiles()
    {
        var repositoryProfiles = Profiles
            .Where(profile => profile.ProfileType == EProfileType.Repository)
            .ToArray();

        foreach (var profile in repositoryProfiles)
        {
            await profile.Update(verbose: false);
        }
    }

    public override async Task OnViewOpened()
    {
        OnPropertyChanged(nameof(CanCreateProfile));
    }

    public async Task CreateProfile()
    {
        var content = new CreateProfileDialog();
        var dialog = new ContentDialog
        {
            Title = "Create New Profile",
            Content = content,
            CloseButtonText = "Cancel",
            PrimaryButtonText = "Create",
            PrimaryButtonCommand = new RelayCommand(async () =>
            {
                if (content.DataContext is not CreateProfileDialogContext dialogContext) return;
                
                InstallationVersion targetVersion;
                if (dialogContext.ProfileType == EProfileType.Repository)
                {
                    var targetDownloadVersion = dialogContext.SelectedRepository.Versions
                        .MaxBy(version => version.UploadTime)!;

                    targetVersion = await targetDownloadVersion.DownloadInstallationVersion();
                }
                else
                {
                    targetVersion = dialogContext.SelectedVersion;
                }
                
                var id = Guid.NewGuid();
                var profilePath = Path.Combine(AppSettings.Current.InstallationPath, id.ToString());
                Directory.CreateDirectory(profilePath);
                
                var profile = new InstallationProfile
                {
                    ProfileType = dialogContext.ProfileType,
                    Name = dialogContext.ProfileType == EProfileType.Repository ? targetVersion.RepositoryName : targetVersion.Name,
                    Version = targetVersion.Version,
                    Directory = profilePath,
                    ExecutableName = Path.GetFileName(targetVersion.ExecutablePath),
                    Id = id,
                    IconUrl = targetVersion.IconUrl,
                    RepositoryUrl = targetVersion.RepositoryUrl
                };
                
                File.Copy(targetVersion.ExecutablePath, profile.ExecutablePath);
                
                ProfilesSource.Add(profile);
                
            })
        };

        await dialog.ShowAsync();
    }

    public async Task ImportInstallation()
    {
        if (await BrowseFileDialog(fileTypes: [Globals.ExecutableFileType]) is not { } executablePath) return;
        
        var content = new CreateProfileDialog();
        var dialog = new ContentDialog
        {
            Title = "Import Installation",
            Content = content,
            CloseButtonText = "Cancel",
            PrimaryButtonText = "Import",
            PrimaryButtonCommand = new RelayCommand(async () =>
            {
                if (content.DataContext is not CreateProfileDialogContext dialogContext) return;

                InstallationVersion targetVersion;
                if (dialogContext.ProfileType == EProfileType.Repository)
                {
                    var targetDownloadVersion = dialogContext.SelectedRepository.Versions
                        .MaxBy(version => version.UploadTime)!;

                    targetVersion = await targetDownloadVersion.DownloadInstallationVersion();
                }
                else
                {
                    targetVersion = dialogContext.SelectedVersion;
                }
                
                var id = Guid.NewGuid();
                
                var profile = new InstallationProfile
                {
                    ProfileType = dialogContext.ProfileType,
                    Name = Path.GetFileNameWithoutExtension(executablePath),
                    Version = targetVersion.Version,
                    Directory = Path.GetDirectoryName(executablePath)!,
                    ExecutableName = Path.GetFileName(executablePath),
                    Id = id,
                    IconUrl = targetVersion.IconUrl,
                    RepositoryUrl = targetVersion.RepositoryUrl
                };
                
                File.Copy(targetVersion.ExecutablePath, profile.ExecutablePath, true);
                
                ProfilesSource.Add(profile);
                
            })
        };
            
        await dialog.ShowAsync();
    }
    

    public async Task Delete(InstallationProfile profile)
    {
        var dialog = new ContentDialog
        {
            Title = $"Delete \"{profile.Name}\"",
            Content = "Are you sure you want to delete this profile?",
            CloseButtonText = "No",
            PrimaryButtonText = "Yes",
            PrimaryButtonCommand = new RelayCommand(async () =>
            {
                await profile.DeleteAndCleanup();
                ProfilesSource.Remove(profile);
            })
        };

        await dialog.ShowAsync();
    }
}