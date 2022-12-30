using System.Threading.Tasks;
using RestSharp;

namespace FortnitePorting.Services.Endpoints;

public class FortnitePortingEndpoint : EndpointBase
{
    public FortnitePortingEndpoint(RestClient client) : base(client) { }

    public async Task<UpdateInfo?> GetReleaseInfoAsync(EUpdateMode updateMode)
    {
        var request = new RestRequest($"https://halfheart.pizza/fortnite-porting/{updateMode.ToString().ToLower()}.json");
        var response = await _client.ExecuteAsync<UpdateInfo>(request).ConfigureAwait(false);
        return response.Data;
    }

    public UpdateInfo? GetReleaseInfo(EUpdateMode updateMode)
    {
        return GetReleaseInfoAsync(updateMode).GetAwaiter().GetResult();
    }
}