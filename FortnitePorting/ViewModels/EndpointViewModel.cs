using FortnitePorting.Framework.ViewModels;
using FortnitePorting.Framework.ViewModels.Endpoints;
using FortnitePorting.ViewModels.Endpoints;

namespace FortnitePorting.ViewModels;

public class EndpointViewModel : EndpointViewModelBase
{
    public readonly FortniteCentralEndpoint FortniteCentral;
    public readonly FortnitePortingEndpoint FortnitePorting;
    public readonly EpicGamesEndpoint EpicGames;
    
    public EndpointViewModel() : base($"FortnitePorting/{Globals.VERSION}")
    {
        FortniteCentral = new FortniteCentralEndpoint(Client);
        FortnitePorting = new FortnitePortingEndpoint(Client);
        EpicGames = new EpicGamesEndpoint(Client);
    }
}