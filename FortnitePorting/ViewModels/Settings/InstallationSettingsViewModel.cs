using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using Newtonsoft.Json;
using InstallationProfile = FortnitePorting.Models.Installation.InstallationProfile;

namespace FortnitePorting.ViewModels.Settings;

public partial class InstallationSettingsViewModel : SettingsViewModelBase
{
    [JsonIgnore] public SupabaseService SupaBase => AppServices.SupaBase;
    
    [ObservableProperty] private bool _finishedSetup;
    [ObservableProperty] private ObservableCollection<InstallationProfile> _profiles = [];
    [ObservableProperty] private bool _canRemoveProfiles;

    [JsonIgnore] public InstallationProfile CurrentProfile => Profiles.First(profile => profile.IsSelected);

    [ObservableProperty]
    [property: JsonIgnore]
    private InstallationProfile _selectedEditProfile;
    
    public override async Task Initialize()
    {
        CanRemoveProfiles = Profiles.Count > 1;
        
        Profiles.CollectionChanged += (sender, args) =>
        {
            CanRemoveProfiles = Profiles.Count > 1;
        };
    }

    public async Task AddProfile()
    {
        var profile = new InstallationProfile { ProfileName = "Unnammed" };

        Profiles.Add(profile);
        SelectedEditProfile = profile;
    }
    
    public async Task RemoveProfile()
    {
        var indexToRemove = Profiles.IndexOf(SelectedEditProfile);
        var isCurrentProfile = SelectedEditProfile.IsSelected;
    
        Profiles.Remove(SelectedEditProfile);
    
        if (isCurrentProfile && Profiles.Count > 0)
        {
            Profiles[0].IsSelected = true;
        }
    
        var newIndex = Math.Min(indexToRemove, Profiles.Count - 1);
        SelectedEditProfile = Profiles[newIndex];
    }
}