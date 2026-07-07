using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CUE4Parse.Utils;
using FortnitePorting.Models.API.Base;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Map;
using Mapster;
using RestSharp;

namespace FortnitePorting.Models.API;

public class FortnitePortingAPI(RestClient client) : APIBase(client)
{
    protected override string BaseURL => "https://api.fortniteporting.app/v2";

    // Content
    public async Task<NewsResponse> News() => await ExecuteAsync<NewsResponse>("content/news");

    public async Task<FeaturedArtResponse> FeaturedArt() => await ExecuteAsync<FeaturedArtResponse>("content/featured-art");
    
    public async Task<BroadcastResponse> Broadcasts() => await ExecuteAsync<BroadcastResponse>("content/broadcasts");

    public async Task<GalleryResponse> Gallery() => await ExecuteAsync<GalleryResponse>("content/gallery");

    public async Task<MapResponse> Maps() => await ExecuteAsync<MapResponse>("content/maps");
    
    // Fortnite
    public async Task<AesResponse?> Aes() => await ExecuteAsync<AesResponse>("fortnite/aes");
    public async Task<MappingsResponse?> Mappings() => await ExecuteAsync<MappingsResponse>("fortnite/mappings");
    
    // Online
    public async Task<AuthResponse?> Auth() => await ExecuteAsync<AuthResponse?>("online/auth");
    public async Task<RepositoryResponse?> Repository() => await ExecuteAsync<RepositoryResponse?>("online/repository");
    public async Task<UserInfoResponse?> UserInfo(string id) => await ExecuteAsync<UserInfoResponse>($"online/users/{id}", verbose: false);
    
    // Telemetry
    public async Task PostError(Exception exception) => await ExecuteAsync("telemetry/errors", Method.Post, verbose: false,
        body: new
        {
            version = Globals.Version.GetDisplayString(),
            message = $"{exception.GetType().FullName}: {exception.Message}",
            stackTrace = exception.StackTrace?.SubstringAfter("at ") ?? "None",
        }
    );
    
    public async Task PostLogin() => await ExecuteAsync("telemetry/logins", Method.Post, verbose: false,
        body: new
        {
            version = Globals.Version.GetDisplayString(),
        }
    );
    
    public async Task PostExports(IEnumerable<string> objectPaths) => await ExecuteAsync("telemetry/exports", Method.Post,
        verbose: false,
        body: new
        {
            objectPaths = objectPaths,
        }
    );
    
    // Chat
    public async Task PostMessage(string text, string? replyId = null, string? imagePath = null) => await ExecuteAsync(
        "chat/messages", Method.Post, verbose: false, notifyRateLimit: true,
        body: new
        {
            text = text,
            application = Globals.ApplicationTag,
            replyId = replyId,
            imagePath = imagePath,
        }
    );

    public async Task EditMessage(string text, string id) => await ExecuteAsync($"chat/messages/{id}", Method.Patch,
        verbose: false, notifyRateLimit: true,
        body: new
        {
            text = text,
        }
    );

    public async Task DeleteMessage(string id) => await ExecuteAsync($"chat/messages/{id}", Method.Delete, verbose: false,
        notifyRateLimit: true
    );

    // Management
    
    public async Task<string> CreateMap(MapInfo mapInfo) => await ExecuteAsync<string>($"management/maps", Method.Post, verbose: false,
        body: mapInfo.Adapt<MapResponseEntry>()
    );
    
    public async Task UpdateMap(MapInfo mapInfo) => await ExecuteAsync($"management/maps/{mapInfo.Id}", Method.Put, verbose: false,
        body: mapInfo.Adapt<MapResponseEntry>()
    );

    public async Task DeleteMap(string id) => await ExecuteAsync($"management/maps/{id}", Method.Delete, verbose: false);

    

   
}