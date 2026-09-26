using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Views.Setup;
using Microsoft.Extensions.DependencyInjection;

namespace FortnitePorting.ViewModels.Setup;

public partial class WelcomeSetupViewModel : ViewModelBase
{
    
    [RelayCommand]
    public async Task Continue()
    {
        Navigation.Setup.Open<ApplicationSetupView>();
    }
}
