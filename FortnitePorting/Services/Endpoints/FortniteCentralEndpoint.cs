using System.Threading.Tasks;
using FortnitePorting.Services.Endpoints.Models;
using RestSharp;

namespace FortnitePorting.Services.Endpoints;

public class FortniteCentralEndpoint : EndpointBase
{
    private const string KEY_URL = "https://fortnitecentral.gmatrixgames.ga/api/v1/aes";
    private const string MAPPINGS_URL = "https://fortnitecentral.gmatrixgames.ga/api/v1/mappings";

    public FortniteCentralEndpoint(RestClient client) : base(client) { }

    public async Task<AesResponse?> GetKeysAsync()
    {
        var request = new RestRequest(KEY_URL);
        var response = await _client.ExecuteAsync<AesResponse>(request).ConfigureAwait(false);
        return response.Data;
    }

    public AesResponse? GetKeys()
    {
        return GetKeysAsync().GetAwaiter().GetResult();
    }

    public async Task<MappingsResponse[]?> GetMappingsAsync()
    {
        var request = new RestRequest(MAPPINGS_URL);
        var response = await _client.ExecuteAsync<MappingsResponse[]>(request).ConfigureAwait(false);
        return response.Data;
    }

    public MappingsResponse[]? GetMappings()
    {
        return GetMappingsAsync().GetAwaiter().GetResult();
    }
}