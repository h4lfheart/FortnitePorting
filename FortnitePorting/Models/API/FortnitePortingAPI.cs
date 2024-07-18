using System;
using System.Net;
using System.Threading.Tasks;
using FortnitePorting.Application;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Help;
using FortnitePorting.Shared;
using Newtonsoft.Json;
using RestSharp;

namespace FortnitePorting.Models.API;

public class FortnitePortingAPI : APIBase
{
    public const string NEWS_URL = "https://halfheart.dev/fortnite-porting/api/v3/news.json"; // i need to buy servers lmao
    public const string FEATURED_URL = "https://halfheart.dev/fortnite-porting/api/v3/featured.json";
    public const string STATS_URL = "https://fortniteporting.halfheart.dev/api/v3/stats";
    public const string HELP_URL = "https://fortniteporting.halfheart.dev/api/v3/help";
    public const string HELP_IMAGE_URL = "https://fortniteporting.halfheart.dev/api/v3/help/image";
    public const string AUTH_GET_URL = "https://fortniteporting.halfheart.dev/api/v3/auth";
    public const string AUTH_USER_URL = "https://fortniteporting.halfheart.dev/api/v3/auth/user";
    public const string AUTH_REDIRECT_URL = "https://fortniteporting.halfheart.dev/api/v3/auth/redirect";
    
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
    
    public async Task<HelpArticle[]?> GetHelpAsync()
    {
        return await ExecuteAsync<HelpArticle[]>(HELP_URL);
    }

    public HelpArticle[]? GetHelp()
    {
        return GetHelpAsync().GetAwaiter().GetResult();
    }
    
    public async Task PostHelpAsync(HelpArticle article)
    {
        await ExecuteAsync(HELP_URL, Method.Post, parameters:
        [
            new QueryParameter("source", JsonConvert.SerializeObject(article)),
            new HeaderParameter("token", AppSettings.Current.Online.Auth!.Token),
        ]);
    }

    public void PostHelp(HelpArticle article)
    {
        PostHelpAsync(article).GetAwaiter().GetResult();
    }
    
    public async Task DeleteHelpAsync(string title)
    {
        await ExecuteAsync(HELP_URL, Method.Delete, parameters:
        [
            new QueryParameter("title", title),
            new HeaderParameter("token", AppSettings.Current.Online.Auth!.Token),
        ]);
    }

    public void DeleteHelp(string title)
    {
        DeleteHelpAsync(title).GetAwaiter().GetResult();
    }
    
    public async Task<string> PostHelpImageAsync(byte[] data, string contentType)
    {
        var response = await ExecuteAsync(HELP_IMAGE_URL, Method.Post, files: [ new ApiFile { PropertyName = "file", Name = "ignoreme.png", Data = data} ], parameters:
        [
            new QueryParameter("contentType", contentType),
            new HeaderParameter("token", AppSettings.Current.Online.Auth!.Token),
        ]);

        return response.Content;
    }

    public string PostHelpImage(byte[] data, string contentType)
    {
        return PostHelpImageAsync(data, contentType).GetAwaiter().GetResult();
    }
    
    public async Task PostStatsAsync()
    {
        await ExecuteAsync(STATS_URL, Method.Post, parameters:
        [
            new HeaderParameter("guid", AppSettings.Current.Online.Identification.Identifier.ToString()),
            new HeaderParameter("version", Globals.Version.ToString()),
        ]);
    }

    public void PostStats()
    {
        PostStatsAsync().GetAwaiter().GetResult();
    }

    public async Task<AuthResponse?> GetAuthAsync(Guid state)
    {
        return await ExecuteAsync<AuthResponse>(AUTH_GET_URL, verbose: false,
            parameters: new QueryParameter("state", state.ToString()));
    }

    public AuthResponse? GetAuth(Guid state)
    {
        return GetAuthAsync(state).GetAwaiter().GetResult();
    }
    
    public async Task<UserResponse?> GetUserAsync(string token)
    {
        return await ExecuteAsync<UserResponse>(AUTH_USER_URL, verbose: false,
            parameters: new HeaderParameter("token", token));
    }

    public UserResponse? GetUser(string token)
    {
        return GetUserAsync(token).GetAwaiter().GetResult();
    }
}