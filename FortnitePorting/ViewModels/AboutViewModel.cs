using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Framework;

namespace FortnitePorting.ViewModels;

public partial class AboutViewModel : ViewModelBase
{
    [RelayCommand]
    public void Discord()
    {
        AppVM.Launch(Globals.DISCORD_URL);
    }
    
    [RelayCommand]
    public void KoFi()
    {
        AppVM.Launch(Globals.KOFI_URL);
    }
    
    [RelayCommand]
    public void GitHub()
    {
        AppVM.Launch(Globals.GITHUB_URL);
    }
}