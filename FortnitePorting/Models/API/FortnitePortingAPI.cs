using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CUE4Parse.Utils;
using FortnitePorting.Models.API.Base;
using FortnitePorting.Models.API.Requests;
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
    
    // Fortnite
    public async Task<AesResponse?> Aes() => await ExecuteAsync<AesResponse>("fortnite/aes");
    public async Task<MappingsResponse?> Mappings() => await ExecuteAsync<MappingsResponse>("fortnite/mappings");
    
    // Auth
    public async Task<AuthResponse?> AuthInfo() => await ExecuteAsync<AuthResponse?>("auth/info");
    
    // Repository
    public async Task<RepositoryResponse?> Repository() => await ExecuteAsync<RepositoryResponse?>("repository");
    
    // Users
    public async Task<UserInfoResponse?> UserInfo(string id) => await ExecuteAsync<UserInfoResponse>($"users/{id}", verbose: false);
    
    public async Task PatchUserPermissions(string id, UserPermissionPatchRequest req) => await ExecuteAsync($"users/{id}/permissions", Method.Patch, verbose: false, body: req);
    
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
    public async Task<ChatMessagesResponse?> GetMessages(DateTime? before = null, int limit = 20)
    {
        var parameters = new List<Parameter> { new QueryParameter("limit", limit.ToString()) };
        if (before is not null)
            parameters.Add(new QueryParameter("before", before.Value.ToUniversalTime().ToString("o")));
        return await ExecuteAsync<ChatMessagesResponse>("chat/messages", verbose: false, parameters: parameters.ToArray());
    }

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
    
    public async Task ReactToMessage(string id) => await ExecuteAsync($"chat/messages/{id}/react", Method.Post,
        verbose: false, notifyRateLimit: true
    );


    // Maps
    public async Task<MapResponse> Maps() => await ExecuteAsync<MapResponse>("maps");
    
    public async Task<string?> CreateMap(MapInfo mapInfo) => await ExecuteAsync<string>($"maps", Method.Post, verbose: false,
        body: mapInfo.Adapt<MapResponseEntry>()
    );
    
    public async Task UpdateMap(MapInfo mapInfo) => await ExecuteAsync($"maps/{mapInfo.Id}", Method.Put, verbose: false,
        body: mapInfo.Adapt<MapResponseEntry>()
    );

    public async Task DeleteMap(string id) => await ExecuteAsync($"maps/{id}", Method.Delete, verbose: false);

    

   
}