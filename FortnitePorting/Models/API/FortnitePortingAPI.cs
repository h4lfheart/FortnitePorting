using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FortnitePorting.Application;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Help;
using FortnitePorting.Models.Leaderboard;
using FortnitePorting.Models.Voting;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Models.API;
using FortnitePorting.Shared.Models.API.Responses;
using Newtonsoft.Json;
using RestSharp;
using Poll = FortnitePorting.Models.Voting.Poll;

namespace FortnitePorting.Models.API;

public class FortnitePortingAPI(RestClient client) : APIBase(client)
{
    public const string NEWS_URL = "https://fortniteporting.halfheart.dev/api/v3/news";
    public const string FEATURED_URL = "https://fortniteporting.halfheart.dev/api/v3/featured";
    
    public const string STATS_URL = "https://fortniteporting.halfheart.dev/api/v3/stats";
    
    public const string HELP_URL = "https://fortniteporting.halfheart.dev/api/v3/help";
    public const string HELP_IMAGE_URL = "https://fortniteporting.halfheart.dev/api/v3/help/image";
    
    public const string LEADERBOARD_USERS_URL = "https://fortniteporting.halfheart.dev/api/v3/leaderboard/users";
    public const string LEADERBOARD_EXPORTS_URL = "https://fortniteporting.halfheart.dev/api/v3/leaderboard/exports";
    public const string LEADERBOARD_EXPORTS_PERSONAL_URL = "https://fortniteporting.halfheart.dev/api/v3/leaderboard/exports/personal";
    public const string LEADERBOARD_STREAKS_URL = "https://fortniteporting.halfheart.dev/api/v3/leaderboard/streaks";
    public const string LEADERBOARD_STREAKS_PERSONAL_URL = "https://fortniteporting.halfheart.dev/api/v3/leaderboard/streaks/personal";
    
    public const string RELEASE_URL = "https://fortniteporting.halfheart.dev/api/v3/release";
    public const string RELEASE_FILES_URL = "https://fortniteporting.halfheart.dev/api/v3/release/files";
    
    public const string REPOSITORY_URL = "https://fortniteporting.halfheart.dev/api/v3/repository";

    public const string POLLS_URL = "https://fortniteporting.halfheart.dev/api/v3/polls";
        
    public const string ONLINE_URL = "https://fortniteporting.halfheart.dev/api/v3/online";
    
    public const string AES_URL = "https://fortniteporting.halfheart.dev/api/v3/aes";
    public const string MAPPINGS_URL = "https://fortniteporting.halfheart.dev/api/v3/mappings";
    
    public async Task<NewsResponse[]> GetNewsAsync()
    {
        return await ExecuteAsync<NewsResponse[]>(NEWS_URL) ?? [];
    }

    public NewsResponse[] GetNews()
    {
        return GetNewsAsync().GetAwaiter().GetResult();
    }
    
    public async Task<FeaturedResponse[]> GetFeaturedAsync()
    {
        return await ExecuteAsync<FeaturedResponse[]>(FEATURED_URL) ?? [];
    }

    public FeaturedResponse[] GetFeatured()
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
            new BodyParameter(JsonConvert.SerializeObject(article), ContentType.Json),
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
        ]);

        return response.Content;
    }

    public string PostHelpImage(byte[] data, string contentType)
    {
        return PostHelpImageAsync(data, contentType).GetAwaiter().GetResult();
    }
    
    public async Task<ReleaseResponse?> GetReleaseAsync()
    {
        return await ExecuteAsync<ReleaseResponse>(RELEASE_URL);
    }

    public ReleaseResponse? GetRelease()
    {
        return GetReleaseAsync().GetAwaiter().GetResult();
    }
    
    public async Task<RepositoryResponse?> GetRepositoryAsync(string url = REPOSITORY_URL)
    {
        return await ExecuteAsync<RepositoryResponse>(url);
    }

    public RepositoryResponse? GetRepository(string url = REPOSITORY_URL)
    {
        return GetRepositoryAsync(url).GetAwaiter().GetResult();
    }
    
    public async Task<string[]> GetReleaseFilesAsync()
    {
        return await ExecuteAsync<string[]>(RELEASE_FILES_URL) ?? [];
    }

    public string[] GetReleaseFiles()
    {
        return GetReleaseFilesAsync().GetAwaiter().GetResult();
    }
    
    public async Task<Poll[]> GetPollsAsync()
    {
        return await ExecuteAsync<Poll[]>(POLLS_URL) ?? [];
    }

    public Poll[] GetPolls()
    {
        return GetPollsAsync().GetAwaiter().GetResult();
    }
    
    public async Task PostVoteAsync(string identifier, string choice)
    {
        await ExecuteAsync(POLLS_URL, Method.Post, parameters:
        [
            new QueryParameter("identifier", identifier),
            new QueryParameter("choice", choice),
        ]);
    }

    public void PostVote(string identifier, string choice)
    {
        PostVoteAsync(identifier, choice).GetAwaiter().GetResult();
    }
    
    public async Task<OnlineResponse?> GetOnlineStatusAsync()
    {
        return await ExecuteAsync<OnlineResponse>(ONLINE_URL);
    }

    public OnlineResponse? GetOnlineStatus()
    {
        return GetOnlineStatusAsync().GetAwaiter().GetResult();
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