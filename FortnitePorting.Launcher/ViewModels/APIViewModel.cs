using FortnitePorting.Launcher.Models.API;
using FortnitePorting.Shared.ViewModels;

namespace FortnitePorting.Launcher.ViewModels;

public class APIViewModel : APIViewModelBase
{
    public FortnitePortingAPI FortnitePorting;
    
    public APIViewModel() : base("FortnitePortingLauncher")
    {
        FortnitePorting = new FortnitePortingAPI(_client);
    }
}