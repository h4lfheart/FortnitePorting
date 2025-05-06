using System.Threading.Tasks;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Shared.Models.API;
using RestSharp;

namespace FortnitePorting.Models.API;

public class FortniteCentralAPI(RestClient client) : APIBase(client)
{
    protected override string BaseURL => "https://fortnitecentral.genxgames.gg/api";
    
    public async Task<AesResponse?> Aes() => await ExecuteAsync<AesResponse?>("v1/aes");
    public async Task<AesResponse?> Aes(string version) => await ExecuteAsync<AesResponse?>("v1/aes", parameters: [
        new QueryParameter("version", version)
    ]);
    
    public async Task<MappingsResponse[]?> Mappings() => await ExecuteAsync<MappingsResponse[]?>("v1/mappings");
    public async Task<MappingsResponse[]?> Mappings(string version) => await ExecuteAsync<MappingsResponse[]?>("v1/mappings", parameters: [
        new QueryParameter("version", version)
    ]);
}