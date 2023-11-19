using System.Threading.Tasks;
using FortnitePorting.Services.Endpoints.Models;
using RestSharp;

namespace FortnitePorting.Services.Endpoints;

public class FortniteCentralEndpoint : EndpointBase
{
    private const string AES_URL = "https://fortnitecentral.genxgames.gg/api/v1/aes";
    private const string MAPPINGS_URL = "https://fortnitecentral.genxgames.gg/api/v1/mappings";

    public FortniteCentralEndpoint(RestClient client) : base(client)
    {
    }

    public async Task<AesResponse?> GetKeysAsync()
    {
        return await ExecuteAsync<AesResponse>(AES_URL);
    }

    public AesResponse? GetKeys()
    {
        return GetKeysAsync().GetAwaiter().GetResult();
    }

    public async Task<MappingsResponse[]?> GetMappingsAsync()
    {
        return await ExecuteAsync<MappingsResponse[]>(MAPPINGS_URL);
    }

    public MappingsResponse[]? GetMappings()
    {
        return GetMappingsAsync().GetAwaiter().GetResult();
    }
}