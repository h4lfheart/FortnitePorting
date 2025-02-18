using System.IO;
using System.Threading.Tasks;
using CUE4Parse.Utils;
using FortnitePorting.Application;
using FortnitePorting.Models.API;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Models.API;
using FortnitePorting.Shared.ViewModels;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace FortnitePorting.ViewModels;

public class APIViewModel : APIViewModelBase
{
    public readonly FortnitePortingAPI FortnitePorting;
    public readonly FortnitePortingServerAPI FortnitePortingServer;
    public readonly FortniteCentralAPI FortniteCentral;
    public readonly EpicGamesAPI EpicGames;


    public APIViewModel() : base(Globals.VersionString, AppSettings.Current.Debug.RequestTimeoutSeconds)
    {
        FortnitePorting = new FortnitePortingAPI(_client);
        FortniteCentral = new FortniteCentralAPI(_client);
        FortnitePortingServer = new FortnitePortingServerAPI(_client);
        EpicGames = new EpicGamesAPI(_client);
    }
    
}