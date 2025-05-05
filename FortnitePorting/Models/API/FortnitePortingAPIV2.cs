using System.Threading.Tasks;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Shared.Models.API;
using RestSharp;

namespace FortnitePorting.Models.API;

public class FortnitePortingAPIV2(RestClient client) : APIBase(client)
{
    protected override string BaseURL => "https://api.fortniteporting.halfheart.dev";

    public async Task<UserInfoResponse?> UserInfo(string id) => await ExecuteAsync<UserInfoResponse>("v1/user", parameters: [
        new QueryParameter(nameof(id), id)
    ]);
    
    public async Task<NewsResponse[]> News() => await ExecuteAsync<NewsResponse[]>("v1/news") ?? [];
    public async Task<FeaturedArtResponse[]> FeaturedArt() => await ExecuteAsync<FeaturedArtResponse[]>("v1/featured_art") ?? [];
}