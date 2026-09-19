using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Framework;
using FortnitePorting.Views.Setup;

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
