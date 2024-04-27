using System.Threading.Tasks;
using FortnitePorting.Models.API;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace FortnitePorting.ViewModels;

public class APIViewModel : ViewModelBase
{
    public readonly FortnitePortingEndpoint FortnitePorting;

    protected readonly RestClient _client;

    private static readonly RestClientOptions _clientOptions = new()
    {
        UserAgent = $"FortnitePorting/{Globals.VersionString}",
        MaxTimeout = 1000 * 10,
    };

    public APIViewModel()
    {
        _client = new RestClient(_clientOptions, configureSerialization: s => s.UseSerializer<JsonNetSerializer>());
        FortnitePorting = new FortnitePortingEndpoint(_client);
    }

}