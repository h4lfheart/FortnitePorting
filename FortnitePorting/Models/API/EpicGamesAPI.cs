using System.Threading.Tasks;
using EpicManifestParser.Api;
using FortnitePorting.Application;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Shared.Models.API;
using RestSharp;

namespace FortnitePorting.Models.API;

public class EpicGamesAPI(RestClient client) : APIBase(client)
{
    private const string OAUTH_POST_URL = "https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token";
    private const string OATH_VERIFY_URL = "https://account-public-service-prod.ol.epicgames.com/account/api/oauth/verify";
    private const string BASIC_TOKEN = "basic ZWM2ODRiOGM2ODdmNDc5ZmFkZWEzY2IyYWQ4M2Y1YzY6ZTFmMzFjMjExZjI4NDEzMTg2MjYyZDM3YTEzZmM4NGQ=";
    private const string FORTNITE_LIVE_URL = "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/v2/platform/Windows/namespace/fn/catalogItem/4fe75bbc5a674f4f9b356b5c90567da5/app/Fortnite/label/Live";

    public async Task<ManifestInfo?> GetManifestInfoAsync()
    {
        var response = await ExecuteAsync(FORTNITE_LIVE_URL, parameters:
        [
            new HeaderParameter("Authorization", $"bearer {AppServices.AppSettings.Application.EpicAuth?.Token}")
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
            new HeaderParameter("Authorization", $"bearer {AppServices.AppSettings.Application.EpicAuth?.Token}")
        ]);

        if (auth is null)
        {
            AppServices.AppSettings.Application.EpicAuth = await GetAuthTokenAsync();
        }
    }
}