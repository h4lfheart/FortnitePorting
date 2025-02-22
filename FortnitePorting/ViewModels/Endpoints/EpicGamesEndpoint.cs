using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using EpicManifestParser;
using EpicManifestParser.Api;
using EpicManifestParser.UE;
using FortnitePorting.Application;
using FortnitePorting.Framework.ViewModels.Endpoints;
using FortnitePorting.ViewModels.Endpoints.Models;
using RestSharp;

namespace FortnitePorting.ViewModels.Endpoints;

public class EpicGamesEndpoint : EndpointBase
{
    public const string CHUNKS_URL = "https://epicgames-download1.akamaized.net/Builds/Fortnite/CloudDir/ChunksV4/";
    private const string OAUTH_POST_URL = "https://account-public-service-prod03.ol.epicgames.com/account/api/oauth/token";
    private const string OATH_VERIFY_URL = "https://account-public-service-prod.ol.epicgames.com/account/api/oauth/verify";
    private const string BASIC_TOKEN = "basic ZWM2ODRiOGM2ODdmNDc5ZmFkZWEzY2IyYWQ4M2Y1YzY6ZTFmMzFjMjExZjI4NDEzMTg2MjYyZDM3YTEzZmM4NGQ=";
    private const string FORTNITE_LIVE_URL = "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/v2/platform/Windows/namespace/fn/catalogItem/4fe75bbc5a674f4f9b356b5c90567da5/app/Fortnite/label/Live";

    public EpicGamesEndpoint(RestClient client) : base(client)
    {
        Task.Run(async () => await VerifyAuthAsync());
    }

    public async Task<byte[]?> GetWithAuth(string url)
    {
        var response = await ExecuteAsync(url, Method.Get,
            new HeaderParameter("Authorization", $"bearer {AppSettings.Current.EpicGamesAuth?.Token}"));
        return response.RawBytes;
    }

    public async Task<ManifestInfo?> GetManifestInfoAsync()
    {
        var response = await ExecuteAsync(FORTNITE_LIVE_URL, Method.Get,
            new HeaderParameter("Authorization", $"bearer {AppSettings.Current.EpicGamesAuth?.Token}"));
        return ManifestInfo.Deserialize(response.RawBytes);
    }

    public ManifestInfo? GetManifestInfo()
    {
        return GetManifestInfoAsync().GetAwaiter().GetResult();
    }

    public async Task<FBuildPatchAppManifest> GetManifestAsync(string url = "", string writePath = "")
    {
        byte[] manifestBytes;
        if (File.Exists(writePath))
        {
            manifestBytes = await File.ReadAllBytesAsync(writePath);
        }
        else
        {
            var response = await ExecuteAsync(url);
            manifestBytes = response.RawBytes!;
            if (!string.IsNullOrEmpty(writePath)) await File.WriteAllBytesAsync(writePath, manifestBytes);
        }


        return FBuildPatchAppManifest.Deserialize(manifestBytes, new ManifestParseOptions
        {
            ChunkBaseUrl = CHUNKS_URL,
            ChunkCacheDirectory = ChunkCacheFolder.FullName,
            CacheChunksAsIs = true
        });
    }

    public FBuildPatchAppManifest GetManifest(string url = "")
    {
        return GetManifestAsync(url).GetAwaiter().GetResult();
    }

    public async Task<AuthResponse?> GetAuthTokenAsync()
    {
        return await ExecuteAsync<AuthResponse>(OAUTH_POST_URL, Method.Post,
            new HeaderParameter("Authorization", BASIC_TOKEN),
            new GetOrPostParameter("grant_type", "client_credentials"));
    }

    public AuthResponse? GetAuthToken()
    {
        return GetAuthTokenAsync().GetAwaiter().GetResult();
    }

    public async Task VerifyAuthAsync()
    {
        var auth = await ExecuteAsync<AuthResponse>(OATH_VERIFY_URL, Method.Get,
            new HeaderParameter("Authorization", $"bearer {AppSettings.Current.EpicGamesAuth?.Token}"));
        if (auth is null) AppSettings.Current.EpicGamesAuth = await GetAuthTokenAsync();
    }
}