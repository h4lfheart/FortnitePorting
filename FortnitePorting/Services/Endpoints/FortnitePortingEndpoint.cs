using System.Threading.Tasks;
using FortnitePorting.Services.Endpoints.Models;
using RestSharp;

namespace FortnitePorting.Services.Endpoints;

public class FortnitePortingEndpoint : EndpointBase
{
    public FortnitePortingEndpoint(RestClient client) : base(client)
    {
    }

    public async Task<UpdateInfo?> GetReleaseInfoAsync(EUpdateMode updateMode)
    {
        var request = new RestRequest($"https://halfheart.dev/fortnite-porting/api/v1/{updateMode.ToString().ToLower()}.json");
        var response = await _client.ExecuteAsync<UpdateInfo>(request).ConfigureAwait(false);
        Log.Information("[{Method}] {StatusDescription} ({StatusCode}): {URI}", request.Method, response.StatusDescription, (int)response.StatusCode, request.Resource);
        return response.Data;
    }

    public UpdateInfo? GetReleaseInfo(EUpdateMode updateMode)
    {
        return GetReleaseInfoAsync(updateMode).GetAwaiter().GetResult();
    }
    
    public async Task<Broadcast?> GetBroadcastAsync()
    {
        var request = new RestRequest($"https://halfheart.dev/fortnite-porting/api/v1/broadcast.json");
        var response = await _client.ExecuteAsync<Broadcast>(request).ConfigureAwait(false);
        Log.Information("[{Method}] {StatusDescription} ({StatusCode}): {URI}", request.Method, response.StatusDescription, (int)response.StatusCode, request.Resource);
        return response.Data;
    }

    public Broadcast? GetBroadcast()
    {
        return GetBroadcastAsync().GetAwaiter().GetResult();
    }
    
    public async Task<BackupAPI?> GetBackupAsync()
    {
        var request = new RestRequest($"https://halfheart.dev/fortnite-porting/api/v1/backup.json");
        var response = await _client.ExecuteAsync<BackupAPI>(request).ConfigureAwait(false);
        Log.Information("[{Method}] {StatusDescription} ({StatusCode}): {URI}", request.Method, response.StatusDescription, (int)response.StatusCode, request.Resource);
        return response.Data;
    }

    public BackupAPI? GetBackup()
    {
        return GetBackupAsync().GetAwaiter().GetResult();
    }
}