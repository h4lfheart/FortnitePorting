using System.Net;
using System.Threading.Tasks;
using FortnitePorting.Application;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Shared;
using Newtonsoft.Json;
using RestSharp;

namespace FortnitePorting.Models.API;

public class FortnitePortingAPI : APIBase
{
    public const string NEWS_URL = "https://halfheart.dev/fortnite-porting/api/v3/news.json"; // i need to buy servers lmao
    public const string FEATURED_URL = "https://halfheart.dev/fortnite-porting/api/v3/featured.json";
    public const string STATS_URL = "https://fortniteporting.halfheart.dev/api/v3/stats";
    public const string DISCORD_GET_URL = "https://fortniteporting.halfheart.dev/api/v3/discord/get";
    public const string DISCORD_POST_URL = "https://fortniteporting.halfheart.dev/api/v3/discord/post";
    public const string DISCORD_REFRESH_URL = "https://fortniteporting.halfheart.dev/api/v3/discord/refresh";
    
    public FortnitePortingAPI(RestClient client) : base(client)
    {
    }

    public async Task<NewsResponse[]?> GetNewsAsync()
    {
        return await ExecuteAsync<NewsResponse[]>(NEWS_URL);
    }

    public NewsResponse[]? GetNews()
    {
        return GetNewsAsync().GetAwaiter().GetResult();
    }
    
    public async Task<FeaturedResponse[]?> GetFeaturedAsync()
    {
        return await ExecuteAsync<FeaturedResponse[]>(FEATURED_URL);
    }

    public FeaturedResponse[]? GetFeatured()
    {
        return GetFeaturedAsync().GetAwaiter().GetResult();
    }
    
    public async Task PostStatsAsync()
    {
        await ExecuteAsync(STATS_URL, Method.Post, parameters:
        [
            new HeaderParameter("guid", AppSettings.Current.Online.Id.ToString()),
            new HeaderParameter("version", Globals.Version.ToString()),
        ]);
    }

    public void PostStats()
    {
        PostStatsAsync().GetAwaiter().GetResult();
    }
    
    public async Task<OAuthResponse?> GetDiscordAuthAsync()
    {
        var response = await ExecuteAsync(DISCORD_GET_URL, verbose: false, parameters: new QueryParameter("id", AppSettings.Current.Online.Id.ToString()));
        return response.StatusCode == HttpStatusCode.OK ? JsonConvert.DeserializeObject<OAuthResponse>(response.Content) : null;
    }

    public OAuthResponse? GetDiscordAuth()
    {
        return GetDiscordAuthAsync().GetAwaiter().GetResult();
    }
    
    public async Task<OAuthResponse?> GetDiscordRefreshAsync()
    {
        var response = await ExecuteAsync(DISCORD_GET_URL, verbose: false, parameters: new QueryParameter("refreshToken", AppSettings.Current.Online.Auth.RefreshToken));
        return response.StatusCode == HttpStatusCode.OK ? JsonConvert.DeserializeObject<OAuthResponse>(response.Content) : null;
    }

    public OAuthResponse? GetDiscordRefresh()
    {
        return GetDiscordRefreshAsync().GetAwaiter().GetResult();
    }
}