using System.Threading.Tasks;
using FortnitePorting.Models.API.Responses;
using RestSharp;

namespace FortnitePorting.Models.API;

public class FortnitePortingAPI : APIBase
{
    public const string NEWS_URL = "https://halfheart.dev/fortnite-porting/api/v3/news.json"; // i need to buy servers lmao
    public const string FEATURED_URL = "https://halfheart.dev/fortnite-porting/api/v3/featured.json";
    
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
}