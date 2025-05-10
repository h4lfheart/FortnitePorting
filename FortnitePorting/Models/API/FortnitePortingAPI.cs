using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Shared.Models.API;
using RestSharp;

namespace FortnitePorting.Models.API;

public class FortnitePortingAPI(RestClient client) : APIBase(client)
{
    protected override string BaseURL => "https://api.fortniteporting.halfheart.dev";
    
    private readonly ConcurrentDictionary<string, UserInfoResponse> _userInfoCache = [];

    public async Task<UserInfoResponse?> UserInfo(string id)
    {
        if (_userInfoCache.TryGetValue(id, out var existingUserInfo)) return existingUserInfo;
        
        var userInfo = await ExecuteAsync<UserInfoResponse>("v1/user", parameters: [
            new QueryParameter(nameof(id), id)
        ]);

        if (userInfo is not null)
            _userInfoCache[id] = userInfo;

        return userInfo;
    }
    
    public async Task<NewsResponse[]> News() => await ExecuteAsync<NewsResponse[]>("v1/news") ?? [];
    public async Task<FeaturedArtResponse[]> FeaturedArt() => await ExecuteAsync<FeaturedArtResponse[]>("v1/featured_art") ?? [];
    
    public async Task<AesResponse?> Aes() => await ExecuteAsync<AesResponse>("v1/static/aes");
    public async Task<MappingsResponse[]?> Mappings() => await ExecuteAsync<MappingsResponse[]?>("v1/static/mappings");
    public async Task<OnlineResponse?> Online() => await ExecuteAsync<OnlineResponse?>("v1/static/online");
    public async Task<RepositoryResponse?> Repository() => await ExecuteAsync<RepositoryResponse?>("v1/static/repository");
    public async Task Message(string text) => await ExecuteAsync("v1/chat/message", Method.Post, parameters: [
        new HeaderParameter("Authorization", $"bearer {SupaBase.Client.Auth.CurrentSession!.AccessToken}"),
        new HeaderParameter("text", text),
        new HeaderParameter("application", Globals.ApplicationTag)
    ]);
}