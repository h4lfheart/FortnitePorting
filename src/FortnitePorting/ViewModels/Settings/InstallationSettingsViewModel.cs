using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Models.Information;
using FortnitePorting.Services;
using FortnitePorting.Views;
using Newtonsoft.Json;
using InstallationProfile = FortnitePorting.Models.Installation.InstallationProfile;

namespace FortnitePorting.ViewModels.Settings;

public partial class InstallationSettingsViewModel : SettingsViewModelBase
{
    [JsonIgnore] public SupabaseService SupaBase => AppServices.SupaBase;
    [JsonIgnore] public CUE4ParseService UEParse => AppServices.UEParse;
    
    [ObservableProperty] private bool _finishedSetup;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRemoveProfiles))]
    private ObservableCollection<InstallationProfile> _profiles = [];

    [JsonIgnore] public bool CanRemoveProfiles => Profiles.Count > 1;

    [JsonIgnore] public InstallationProfile CurrentProfile => Profiles.FirstOrDefault(profile => profile.IsSelected);

    [ObservableProperty]
    [property: JsonIgnore]
    private InstallationProfile _selectedEditProfile;

    public override async Task Initialize()
    {
        Profiles.CollectionChanged += (sender, args) => OnPropertyChanged(nameof(CanRemoveProfiles));
    }

    [RelayCommand]
    public async Task AddProfile()
    {
        var profile = new InstallationProfile { ProfileName = "Unnammed" };

        Profiles.Add(profile);
        SelectedEditProfile = profile;
    }

    [RelayCommand]
    public async Task DuplicateProfile(InstallationProfile? profile = null)
    {
        profile ??= SelectedEditProfile;
        if (profile is null) return;

        var duplicate = JsonConvert.DeserializeObject<InstallationProfile>(JsonConvert.SerializeObject(profile))!;
        duplicate.ProfileName = $"{profile.ProfileName} Copy";
        duplicate.IsSelected = false;

        Profiles.Add(duplicate);
        SelectedEditProfile = duplicate;
    }
    
    [RelayCommand]
    public async Task RemoveProfile(InstallationProfile? profile = null)
    {
        profile ??= SelectedEditProfile;
        if (profile is null || !CanRemoveProfiles) return;

        var profileToRemove = profile;
        var wasEditing = SelectedEditProfile == profileToRemove;
        Info.Dialog("Delete Profile",
            $"Are you sure you want to delete \"{profileToRemove.ProfileName}\"? This cannot be undone.",
            buttons:
            [
                new DialogButton
                {
                    Text = "Delete",
                    IsPrimary = true,
                    Action = () =>
                    {
                        var indexToRemove = Profiles.IndexOf(profileToRemove);
                        if (indexToRemove < 0) return;

                        var isCurrentProfile = profileToRemove.IsSelected;
                        Profiles.Remove(profileToRemove);

                        if (isCurrentProfile && Profiles.Count > 0)
                        {
                            Profiles[0].IsSelected = true;
                        }

                        if (wasEditing)
                        {
                            var newIndex = Math.Min(indexToRemove, Profiles.Count - 1);
                            SelectedEditProfile = Profiles[newIndex];
                        }
                    }
                },
                new DialogButton
                {
                    Text = "Cancel"
                }
            ]);
    }

    [RelayCommand]
    public async Task ReloadInstallation()
    {
        Info.Dialog("Reload Installation",
            "Would you like to reload the installation session with the current profile settings? Loaded file data will be reset.",
            buttons:
            [
                new DialogButton
                {
                    Text = "Reload",
                    IsPrimary = true,
                    Action = () => TaskService.Run(async () =>
                    {
                        AppSettings.Save();
                        Navigation.App.Open<HomeView>();
                        await App.ReloadInstallationAsync();
                    })
                },
                new DialogButton
                {
                    Text = "Cancel"
                }
            ]);
    }
}