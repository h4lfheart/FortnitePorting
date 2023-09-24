using System.Threading.Tasks;
using FortnitePorting.Services.Endpoints.Models;
using RestSharp;

namespace FortnitePorting.Services.Endpoints;

public class FortnitePortingEndpoint : EndpointBase
{
    private const string RELEASE_URL = "https://halfheart.dev/fortnite-porting/api/v1/release.json";
    private const string BROADCAST_URL = "https://halfheart.dev/fortnite-porting/api/v1/broadcast.json";
    private const string BACKUP_API_URL = "https://halfheart.dev/fortnite-porting/api/v1/backup.json";
    
    public FortnitePortingEndpoint(RestClient client) : base(client)
    {
    }

    public async Task<ReleaseResponse?> GetReleaseAsync() => await ExecuteAsync<ReleaseResponse>(RELEASE_URL);
    public ReleaseResponse? GetReleaseInfo() => GetReleaseAsync().GetAwaiter().GetResult();

    public async Task<BroadcastResponse[]?> GetBroadcastsAsync() => await ExecuteAsync<BroadcastResponse[]>(BROADCAST_URL);
    public BroadcastResponse[]? GetBroadcasts() => GetBroadcastsAsync().GetAwaiter().GetResult();

    public async Task<BackupAPIResponse?> GetBackupAPIAsync() => await ExecuteAsync<BackupAPIResponse>(BACKUP_API_URL);

    public BackupAPIResponse? GetBackupAPI() => GetBackupAPIAsync().GetAwaiter().GetResult();
}