using System.Threading.Tasks;
using EpicManifestParser.Api;
using FortnitePorting.Application;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Shared.Models.API;
using RestSharp;

namespace FortnitePorting.Models.API;

public class EpicGamesAPI(RestClient client) : APIBase(client)
{
    public const string CHUNKS_URL = "https://epicgames-download1.akamaized.net/Builds/Fortnite/CloudDir/ChunksV4/";
    private const string OAUTH_POST_URL = "https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token";
    private const string OATH_VERIFY_URL = "https://account-public-service-prod.ol.epicgames.com/account/api/oauth/verify";
    private const string BASIC_TOKEN = "basic MzQ0NmNkNzI2OTRjNGE0NDg1ZDgxYjc3YWRiYjIxNDE6OTIwOWQ0YTVlMjVhNDU3ZmI5YjA3NDg5ZDMxM2I0MWE=";
    private const string FORTNITE_LIVE_URL = "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/v2/platform/Windows/namespace/fn/catalogItem/4fe75bbc5a674f4f9b356b5c90567da5/app/Fortnite/label/Live";

    public async Task<ManifestInfo?> GetManifestInfoAsync()
    {
        var response = await ExecuteAsync(FORTNITE_LIVE_URL, parameters:
        [
            new HeaderParameter("Authorization", $"bearer {AppSettings.Current.Online.EpicAuth?.Token}")
        ]);
        
        return ManifestInfo.Deserialize(response.RawBytes);
    }

    public ManifestInfo? GetManifestInfo()
    {
        return GetManifestInfoAsync().GetAwaiter().GetResult();
    }
    
    public async Task<EpicAuthResponse?> GetAuthTokenAsync()
    {
        return await ExecuteAsync<EpicAuthResponse>(OAUTH_POST_URL, Method.Post, parameters:
        [
            new HeaderParameter("Authorization", BASIC_TOKEN),
            new GetOrPostParameter("grant_type", "client_credentials")
        ]);
    }

    public EpicAuthResponse? GetAuthToken()
    {
        return GetAuthTokenAsync().GetAwaiter().GetResult();
    }

    public async Task VerifyAuthAsync()
    {
        var auth = await ExecuteAsync<EpicAuthResponse>(OATH_VERIFY_URL, parameters:
        [
            new HeaderParameter("Authorization", $"bearer {AppSettings.Current.Online.EpicAuth?.Token}")
        ]);

        if (auth is null)
        {
            AppSettings.Current.Online.EpicAuth = await GetAuthTokenAsync();
        }
    }
}