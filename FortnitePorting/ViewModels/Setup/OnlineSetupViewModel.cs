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
using FortnitePorting.Views;
using FortnitePorting.Views.Setup;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;

namespace FortnitePorting.ViewModels.Setup;

public partial class OnlineSetupViewModel : ViewModelBase
{
    [RelayCommand]
    public async Task Skip()
    {
        Navigation.Setup.Open<FinishedSetupView>();
    }

    [RelayCommand]
    public async Task SignIn()
    {
        await SupaBase.SignIn();
        Navigation.Setup.Open<FinishedSetupView>();
    }
}
