using System.Threading.Tasks;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Shared.Models.API;
using RestSharp;

namespace FortnitePorting.Models.API;

public class FortniteCentralAPI : APIBase
{
    private const string AES_URL = "https://fortnitecentral.genxgames.gg/api/v1/aes";
    private const string MAPPINGS_URL = "https://fortnitecentral.genxgames.gg/api/v1/mappings";
    
    public FortniteCentralAPI(RestClient client) : base(client)
    {
    }

    public async Task<AesResponse?> GetKeysAsync(string version = "")
    {
        Parameter[] parameters = !string.IsNullOrWhiteSpace(version) ? [new QueryParameter("version", version)] : [];
        return await ExecuteAsync<AesResponse>(AES_URL, parameters: parameters);
    }

    public AesResponse? GetKeys(string version = "")
    {
        return GetKeysAsync(version).GetAwaiter().GetResult();
    }

    public async Task<MappingsResponse[]?> GetMappingsAsync(string version = "")
    {
        Parameter[] parameters = !string.IsNullOrWhiteSpace(version) ? [new QueryParameter("version", version)] : [];
        return await ExecuteAsync<MappingsResponse[]>(MAPPINGS_URL, parameters: parameters);
    }

    public MappingsResponse[]? GetMappings(string version = "")
    {
        return GetMappingsAsync(version).GetAwaiter().GetResult();
    }
}