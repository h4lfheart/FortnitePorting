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
    public const string CONTENT_BUILDS_URL = "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/Windows/5cb97847cee34581afdbc445400e2f77/FortniteContentBuilds";

    public EpicEndpoint(RestClient client) : base(client)
    {
    }

    public async Task<ManifestInfo> GetManifestInfoAsync(string url = FORTNITE_LIVE_URL)
    {
        await VerifyAuthAsync();

        var request = new RestRequest(url);
        request.AddHeader("Authorization", $"bearer {AppSettings.Current.EpicAuth?.AccessToken}");

        var response = await _client.ExecuteAsync(request);
        Log.Information("[{Method}] {StatusDescription} ({StatusCode}): {URI}", request.Method, response.StatusDescription, (int)response.StatusCode, request.Resource);
        return new ManifestInfo(response.Content);
    }

    public ManifestInfo GetManifestInfo(string url = FORTNITE_LIVE_URL)
    {
        return GetManifestInfoAsync(url).GetAwaiter().GetResult();
    }

    public async Task<Manifest> GetManifestAsync(string url = "")
    {
        var request = new RestRequest(url);
        var response = await _client.ExecuteAsync(request);
        Log.Error("Manifest Get Errors:");
        Log.Error(response.ErrorException?.ToString() ?? "No Stack Trace.");
        Log.Error(response.ErrorMessage ?? "No Error Message.");
        Log.Information("[{Method}] {StatusDescription} ({StatusCode}): {URI}", request.Method, response.StatusDescription, (int)response.StatusCode, request.Resource);
        return new Manifest(response.RawBytes, new ManifestOptions
        {
            ChunkBaseUri = new Uri("https://epicgames-download1.akamaized.net/Builds/Fortnite/Content/CloudDir/ChunksV4/", UriKind.Absolute),
            ChunkCacheDirectory = App.CacheFolder
        });
    }

    public Manifest GetManifest(string url = "")
    {
        return GetManifestAsync(url).GetAwaiter().GetResult();
    }

    private async Task<EpicAuthResponse?> GetAuthTokenAsync()
    {
        var request = new RestRequest(OAUTH_URL, Method.Post);
        request.AddHeader("Authorization", BASIC_TOKEN);
        request.AddParameter("grant_type", "client_credentials");

        var response = await _client.ExecuteAsync<EpicAuthResponse>(request);
        Log.Information("[{Method}] {StatusDescription} ({StatusCode}): {URI}", request.Method, response.StatusDescription, (int)response.StatusCode, request.Resource);
        return response.Data;
    }

    public EpicAuthResponse? GetAuthToken()
    {
        return GetAuthTokenAsync().GetAwaiter().GetResult();
    }

    public async Task<ContentBuildsResponse?> GetContentBuildsAsync(string url = CONTENT_BUILDS_URL, string label = "")
    {
        await VerifyAuthAsync();

        var request = new RestRequest(url);
        request.AddHeader("Authorization", $"bearer {AppSettings.Current.EpicAuth?.AccessToken}");
        request.AddQueryParameter("label", label);

        var response = await _client.ExecuteAsync<ContentBuildsResponse>(request);
        Log.Information("[{Method}] {StatusDescription} ({StatusCode}): {URI}", request.Method, response.StatusDescription, (int)response.StatusCode, request.Resource);
        return response.Data;
    }

    public ContentBuildsResponse? GetContentBuilds(string url = FORTNITE_LIVE_URL, string label = "")
    {
        return GetContentBuildsAsync(url, label).GetAwaiter().GetResult();
    }

    public async Task<bool> VerifyAuthAsync()
    {
        var authExpired = IsAuthExpired();
        if (authExpired)
        {
            AppSettings.Current.EpicAuth = await GetAuthTokenAsync();
        }

        return authExpired;
    }

    public bool VerifyAuth()
    {
        return VerifyAuthAsync().GetAwaiter().GetResult();
    }

    public bool IsAuthExpired()
    {
        var request = new RestRequest("https://account-public-service-prod.ol.epicgames.com/account/api/oauth/verify");
        request.AddHeader("Authorization", $"bearer {AppSettings.Current.EpicAuth?.AccessToken}");
        var response = _client.Execute(request);
        Log.Information("[{Method}] {StatusDescription} ({StatusCode}): {URI}", request.Method, response.StatusDescription, (int)response.StatusCode, request.Resource);
        return response.StatusCode != HttpStatusCode.OK;
    }
}