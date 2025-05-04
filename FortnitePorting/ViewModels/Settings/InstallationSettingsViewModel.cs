using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Versions;
using FortnitePorting.Framework;
using FortnitePorting.Models.CUE4Parse;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Validators;
using Newtonsoft.Json;
using InstallationProfile = FortnitePorting.Models.Installation.InstallationProfile;

namespace FortnitePorting.ViewModels.Settings;

public partial class InstallationSettingsViewModel : ViewModelBase
{
    [ObservableProperty] private bool _finishedSetup;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CurrentProfile))] private int _currentProfileIndex;
    [ObservableProperty] private ObservableCollection<InstallationProfile> _profiles = [];
    [ObservableProperty] private bool _canRemoveProfiles;

    public InstallationProfile CurrentProfile => CurrentProfileIndex < Profiles.Count && CurrentProfileIndex >= 0 ? Profiles[CurrentProfileIndex] : null;

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
        CurrentProfileIndex = Profiles.IndexOf(profile);
    }
    
    public async Task RemoveProfile()
    {
        var selectedIndexToRemove = CurrentProfileIndex;
        Profiles.RemoveAt(selectedIndexToRemove);
        CurrentProfileIndex = selectedIndexToRemove == 0 ? 0 : selectedIndexToRemove - 1;
    }
}