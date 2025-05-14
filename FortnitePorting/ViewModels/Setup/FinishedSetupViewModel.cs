using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Models.Installation;
using FortnitePorting.Shared.Services;
using FortnitePorting.Views;
using FortnitePorting.Views.Setup;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;

namespace FortnitePorting.ViewModels.Setup;

public partial class FinishedSetupViewModel : ViewModelBase
{
    [RelayCommand]
    public async Task Continue()
    {
        AppSettings.Installation.FinishedSetup = true;
        AppSettings.Application.NextKofiAskDate = DateTime.Today.AddDays(7);
        Navigation.App.Open<HomeView>();
        AppSettings.Save();
    }
}
