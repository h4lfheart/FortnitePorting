using System.Net;
using System.Threading.Tasks;
using EpicManifestParser.Objects;
using FortnitePorting.Application;
using FortnitePorting.Services.Endpoints.Models;
using RestSharp;
using Serilog;

namespace FortnitePorting.Services.Endpoints;

public class EpicGamesEndpoint : EndpointBase
{
    private const string OAUTH_POST_URL = "https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token";
    private const string OATH_VERIFY_URL = "https://account-public-service-prod.ol.epicgames.com/account/api/oauth/verify";
    private const string BASIC_TOKEN = "basic MzQ0NmNkNzI2OTRjNGE0NDg1ZDgxYjc3YWRiYjIxNDE6OTIwOWQ0YTVlMjVhNDU3ZmI5YjA3NDg5ZDMxM2I0MWE=";
    private const string FORTNITE_LIVE_URL = "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/v2/platform/Windows/namespace/fn/catalogItem/4fe75bbc5a674f4f9b356b5c90567da5/app/Fortnite/label/Live";
    
    public EpicGamesEndpoint(RestClient client) : base(client) { }

    public async Task<ManifestInfo?> GetManifestInfoAsync()
    {
        await VerifyAuthAsync();
        var response = await ExecuteAsync(FORTNITE_LIVE_URL, Method.Get,
            new HeaderParameter("Authorization", $"bearer {AppSettings.Current.EpicGamesAuth?.Token}"));
        return new ManifestInfo(response.Content);
    }

    public ManifestInfo? GetManifestInfo() => GetManifestInfoAsync().GetAwaiter().GetResult();

    public async Task VerifyAuthAsync()
    {
        var auth = await ExecuteAsync<AuthResponse>(OATH_VERIFY_URL, Method.Get, 
            new HeaderParameter("Authorization", $"bearer {AppSettings.Current.EpicGamesAuth?.Token}"));
        if (auth is null) AppSettings.Current.EpicGamesAuth = await GetAuthTokenAsync();
    }

    public async Task<AuthResponse?> GetAuthTokenAsync() => await ExecuteAsync<AuthResponse>(OAUTH_POST_URL, Method.Post, 
        new HeaderParameter("Authorization", BASIC_TOKEN), 
        new GetOrPostParameter("grant_type", "client_credentials"));

    public AuthResponse? GetAuthToken() => GetAuthTokenAsync().GetAwaiter().GetResult();
}