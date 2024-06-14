using System.Threading.Tasks;
using FortnitePorting.Application;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Shared;
using RestSharp;

namespace FortnitePorting.Models.API;

public class FortnitePortingAPI : APIBase
{
    public const string NEWS_URL = "https://halfheart.dev/fortnite-porting/api/v3/news.json"; // i need to buy servers lmao
    public const string FEATURED_URL = "https://halfheart.dev/fortnite-porting/api/v3/featured.json";
    public const string STATS_URL = "https://fortniteporting.halfheart.dev/api/v3/stats";
    
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
        await ExecuteAsync(STATS_URL, Method.Post, 
        [
            new HeaderParameter("guid", AppSettings.Current.UUID.ToString()),
            new HeaderParameter("version", Globals.Version.ToString()),
        ]);
    }

    public void PostStats()
    {
        PostStatsAsync().GetAwaiter().GetResult();
    }
}