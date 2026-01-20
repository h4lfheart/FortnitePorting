using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Framework;
using FortnitePorting.Models.Installation;
using FortnitePorting.Services;
using FortnitePorting.Views.Setup;
using Newtonsoft.Json;
using Serilog;

namespace FortnitePorting.ViewModels.Setup;

public partial class ApplicationSetupViewModel() : ViewModelBase
{
    [ObservableProperty] private SettingsService _settings;

    public ApplicationSetupViewModel(SettingsService settings) : this()
    {
        Settings = settings;
    }
    
    [RelayCommand]
    public async Task Continue()
    {
        if (AppSettings.Installation.Profiles.Count > 0)
            Navigation.Setup.Open<OnlineSetupView>();
        else
            Navigation.Setup.Open<InstallationSetupView>();
    }
}