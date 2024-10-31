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
    
    public const string AUTH_GET_URL = "https://fortniteporting.halfheart.dev/api/v3/auth";
    public const string AUTH_USER_URL = "https://fortniteporting.halfheart.dev/api/v3/auth/user";
    public const string AUTH_REDIRECT_URL = "https://fortniteporting.halfheart.dev/api/v3/auth/redirect";
    public const string AUTH_REFRESH_URL = "https://fortniteporting.halfheart.dev/api/v3/auth/refresh";
    
    public const string LEADERBOARD_USERS_URL = "https://fortniteporting.halfheart.dev/api/v3/leaderboard/users";
    public const string LEADERBOARD_EXPORTS_URL = "https://fortniteporting.halfheart.dev/api/v3/leaderboard/exports";
    public const string LEADERBOARD_EXPORTS_PERSONAL_URL = "https://fortniteporting.halfheart.dev/api/v3/leaderboard/exports/personal";
    
    public const string RELEASE_URL = "https://fortniteporting.halfheart.dev/api/v3/release";
    public const string RELEASE_FILES_URL = "https://fortniteporting.halfheart.dev/api/v3/release/files";

    public const string POLLS_URL = "https://fortniteporting.halfheart.dev/api/v3/polls";
        
    public const string ONLINE_URL = "https://fortniteporting.halfheart.dev/api/v3/online";


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
            new HeaderParameter("token", AppSettings.Current.Online.Auth.Token),
            new HeaderParameter("version", Globals.Version.ToString()),
        ]);
    }

    public void PostStats()
    {
        PostStatsAsync().GetAwaiter().GetResult();
    }

    public async Task<AuthResponse?> GetAuthAsync(Guid state)
    {
        return await ExecuteAsync<AuthResponse>(AUTH_GET_URL,
            parameters: new QueryParameter("state", state.ToString()));
    }

    public AuthResponse? GetAuth(Guid state)
    {
        return GetAuthAsync(state).GetAwaiter().GetResult();
    }
    
    public async Task<UserResponse?> GetUserAsync(string token)
    {
        return await ExecuteAsync<UserResponse>(AUTH_USER_URL,
            parameters: new HeaderParameter("token", token));
    }

    public UserResponse? GetUser(string token)
    {
        return GetUserAsync(token).GetAwaiter().GetResult();
    }
    
    public async Task PostExportsAsync(IEnumerable<PersonalExport> exports)
    {
        await ExecuteAsync(LEADERBOARD_EXPORTS_URL, Method.Post, verbose: false, parameters: 
        [
            new BodyParameter(JsonConvert.SerializeObject(exports), ContentType.Json),
            new HeaderParameter("token", AppSettings.Current.Online.Auth!.Token)
        ]);
    }

    public void PostExports(IEnumerable<PersonalExport> exports)
    {
        PostExportsAsync(exports).GetAwaiter().GetResult();
    }
    
    public async Task PostExportAsync(PersonalExport export)
    {
        PersonalExport[] exports = [export];
        
        await ExecuteAsync(LEADERBOARD_EXPORTS_URL, Method.Post, verbose: false, parameters: 
        [
            new BodyParameter(JsonConvert.SerializeObject(exports), ContentType.Json),
            new HeaderParameter("token", AppSettings.Current.Online.Auth!.Token)
        ]);
    }

    public void PostExport(PersonalExport export)
    {
        PostExportAsync(export).GetAwaiter().GetResult();
    }
    
    public async Task<LeaderboardUser[]> GetLeaderboardUsersAsync(int min = 1, int max = 10)
    {
        return await ExecuteAsync<LeaderboardUser[]>(LEADERBOARD_USERS_URL, verbose: false, parameters: 
        [
            new QueryParameter("min", min.ToString()),
            new QueryParameter("max", max.ToString())
        ]) ?? [];
    }

    public LeaderboardUser[] GetLeaderboardUsers(int min = 1, int max = 10)
    {
        return GetLeaderboardUsersAsync(min, max).GetAwaiter().GetResult();
    }
    
    public async Task<LeaderboardExport[]> GetLeaderboardExportsAsync(int min = 1, int max = 10)
    {
        return await ExecuteAsync<LeaderboardExport[]>(LEADERBOARD_EXPORTS_URL, verbose: false, parameters: 
        [
            new QueryParameter("min", min.ToString()),
            new QueryParameter("max", max.ToString())
        ]) ?? [];
    }

    public LeaderboardExport[] GetLeaderboardExports(int min = 1, int max = 10)
    {
        return GetLeaderboardExportsAsync(min, max).GetAwaiter().GetResult();
    }
    
    public async Task<PersonalExport[]> GetPersonalExportsAsync()
    {
        return await ExecuteAsync<PersonalExport[]>(LEADERBOARD_EXPORTS_PERSONAL_URL, verbose: false, parameters: 
        [
            new HeaderParameter("token", AppSettings.Current.Online.Auth!.Token)
        ]) ?? [];
    }

    public PersonalExport[] GetPersonalExports()
    {
        return GetPersonalExportsAsync().GetAwaiter().GetResult();
    }
    
    public async Task<ReleaseResponse?> GetReleaseAsync()
    {
        return await ExecuteAsync<ReleaseResponse>(RELEASE_URL);
    }

    public ReleaseResponse? GetRelease()
    {
        return GetReleaseAsync().GetAwaiter().GetResult();
    }
    
    public async Task<string[]> GetReleaseFilesAsync()
    {
        return await ExecuteAsync<string[]>(RELEASE_FILES_URL) ?? [];
    }

    public string[] GetReleaseFiles()
    {
        return GetReleaseFilesAsync().GetAwaiter().GetResult();
    }
    
    public async Task RefreshAuthAsync()
    {
        await ExecuteAsync(AUTH_REFRESH_URL, Method.Post, parameters: 
        [
            new HeaderParameter("token", AppSettings.Current.Online.Auth!.Token)
        ]);
    }

    public void RefreshAuth()
    {
        RefreshAuthAsync().GetAwaiter().GetResult();
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
            new HeaderParameter("token", AppSettings.Current.Online.Auth!.Token)
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
}