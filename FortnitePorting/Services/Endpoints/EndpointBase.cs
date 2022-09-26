using RestSharp;

namespace FortnitePorting.Services.Endpoints;

public abstract class EndpointBase
{
    protected RestClient _client;

    protected EndpointBase(RestClient client)
    {
        _client = client;
    }
}