using System;
using System.Net;
using System.Threading.Tasks;
using EpicManifestParser.Objects;
using FortnitePorting.AppUtils;
using FortnitePorting.Services.Endpoints.Models;
using RestSharp;

namespace FortnitePorting.Services.Endpoints;

public class EpicEndpoint : EndpointBase
{
    private const string OAUTH_URL = "https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token";
    private const string BASIC_TOKEN = "basic MzQ0NmNkNzI2OTRjNGE0NDg1ZDgxYjc3YWRiYjIxNDE6OTIwOWQ0YTVlMjVhNDU3ZmI5YjA3NDg5ZDMxM2I0MWE=";
    private const string FORTNITE_LIVE_URL = "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/v2/platform/Windows/namespace/fn/catalogItem/4fe75bbc5a674f4f9b356b5c90567da5/app/Fortnite/label/Live";

    public EpicEndpoint(RestClient client) : base(client)
    {
    }

    public async Task<ManifestInfo> GetMainfestAsync(string url = FORTNITE_LIVE_URL)
    {
        if (IsAuthExpired())
        {
            AppSettings.Current.EpicAuth = await GetAuthTokenAsync();
        }
        
        var request = new RestRequest(url);
        request.AddHeader("Authorization", $"bearer {AppSettings.Current.EpicAuth?.AccessToken}");

        var response = await _client.ExecuteAsync(request);
        return new ManifestInfo(response.Content);
    }
    
    public ManifestInfo GetMainfest(string url = FORTNITE_LIVE_URL)
    {
        return GetMainfestAsync().GetAwaiter().GetResult();
    }

    private async Task<EpicAuthResponse?> GetAuthTokenAsync()
    {
        var request = new RestRequest(OAUTH_URL, Method.Post);
        request.AddHeader("Authorization", BASIC_TOKEN);
        request.AddParameter("grant_type", "client_credentials");

        var response = await _client.ExecuteAsync<EpicAuthResponse>(request);
        return response.Data;
    }
    
    public EpicAuthResponse? GetAuthToken()
    {
        return GetAuthTokenAsync().GetAwaiter().GetResult();
    }

    private bool IsAuthExpired()
    {
        var request = new RestRequest("https://account-public-service-prod.ol.epicgames.com/account/api/oauth/verify");
        request.AddHeader("Authorization", $"bearer {AppSettings.Current.EpicAuth?.AccessToken}");
        var response = _client.Execute(request);
        return response.StatusCode != HttpStatusCode.OK;
    }
    
}