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
using FortnitePorting.Views.Setup;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;

namespace FortnitePorting.ViewModels.Setup;

public partial class WelcomeSetupViewModel : ViewModelBase
{
    
    [RelayCommand]
    public async Task Continue()
    {
        AppServices.Services.GetRequiredService<SetupViewModel>().UseBlur = true;
        Navigation.Setup.Open<InstallationSetupView>();
    }
}
