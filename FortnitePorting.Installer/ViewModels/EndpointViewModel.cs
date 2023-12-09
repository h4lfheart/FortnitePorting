using FortnitePorting.Framework.ViewModels;
using FortnitePorting.Framework.ViewModels.Endpoints;

namespace FortnitePorting.Installer.ViewModels;

public class EndpointViewModel : EndpointViewModelBase
{
    public FortnitePortingEndpoint FortnitePorting;
    public EndpointViewModel() : base($"FortnitePorting/{Globals.VERSION}")
    {
        FortnitePorting = new FortnitePortingEndpoint(Client);
    }
}