using System.Threading.Tasks;
using FortnitePorting.Services.Endpoints.Models;
using RestSharp;

namespace FortnitePorting.Services.Endpoints;

public class FortnitePortingEndpoint : EndpointBase
{
    private const string NORMAL_DOMAIN = "https://halfheart.dev/fortnite-porting/api/v1/";
    private const string FALLBACK_DOMAIN = "https://raw.githubusercontent.com/halfuwu/halfheart.dev/master/docs/fortnite-porting/api/v1/";
    
    public FortnitePortingEndpoint(RestClient client) : base(client)
    {
    }

    public async Task<UpdateInfo?> GetReleaseInfoAsync(string domain = NORMAL_DOMAIN)
    {
        var request = new RestRequest(domain + "release.json");
        var response = await _client.ExecuteAsync<UpdateInfo>(request).ConfigureAwait(false);
        Log.Information("[{Method}] {StatusDescription} ({StatusCode}): {URI}", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
        if (domain == FALLBACK_DOMAIN) return response.Data;
        return response.Data ?? await GetReleaseInfoAsync(FALLBACK_DOMAIN);
    }

    public UpdateInfo? GetReleaseInfo()
    {
        return GetReleaseInfoAsync().GetAwaiter().GetResult();
    }

    public async Task<Broadcast[]?> GetBroadcastsAsync(string domain = NORMAL_DOMAIN)
    {
        var request = new RestRequest(domain + "broadcast.json");
        var response = await _client.ExecuteAsync<Broadcast[]>(request).ConfigureAwait(false);
        Log.Information("[{Method}] {StatusDescription} ({StatusCode}): {URI}", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
        if (domain == FALLBACK_DOMAIN) return response.Data;
        return response.Data ?? await GetBroadcastsAsync(FALLBACK_DOMAIN);
    }

    public Broadcast[]? GetBroadcasts()
    {
        return GetBroadcastsAsync().GetAwaiter().GetResult();
    }

    public async Task<BackupAPI?> GetBackupAsync(string domain = NORMAL_DOMAIN)
    {
        var request = new RestRequest(domain + "backup.json");
        var response = await _client.ExecuteAsync<BackupAPI>(request).ConfigureAwait(false);
        Log.Information("[{Method}] {StatusDescription} ({StatusCode}): {URI}", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
        if (domain == FALLBACK_DOMAIN) return response.Data;
        return response.Data ?? await GetBackupAsync(FALLBACK_DOMAIN);
    }

    public BackupAPI? GetBackup()
    {
        return GetBackupAsync().GetAwaiter().GetResult();
    }
}