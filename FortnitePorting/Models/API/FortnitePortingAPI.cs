using System.Threading.Tasks;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Shared.Models.API;
using RestSharp;

namespace FortnitePorting.Models.API;

public class FortnitePortingAPI(RestClient client) : APIBase(client)
{
    public const string RELEASE_FILES_URL = "https://fortniteporting.halfheart.dev/api/v3/release/files";
    
    public const string REPOSITORY_URL = "https://fortniteporting.halfheart.dev/api/v3/repository";
        
    public const string ONLINE_URL = "https://fortniteporting.halfheart.dev/api/v3/online";
    
    public const string AES_URL = "https://fortniteporting.halfheart.dev/api/v3/aes";
    public const string MAPPINGS_URL = "https://fortniteporting.halfheart.dev/api/v3/mappings";
    
    
    public async Task<RepositoryResponse?> GetRepositoryAsync(string url = REPOSITORY_URL)
    {
        return await ExecuteAsync<RepositoryResponse>(url);
    }

    public RepositoryResponse? GetRepository(string url = REPOSITORY_URL)
    {
        return GetRepositoryAsync(url).GetAwaiter().GetResult();
    }
    
    public async Task<string[]> GetReleaseFilesAsync()
    {
        return await ExecuteAsync<string[]>(RELEASE_FILES_URL) ?? [];
    }

    public string[] GetReleaseFiles()
    {
        return GetReleaseFilesAsync().GetAwaiter().GetResult();
    }
    
    public async Task<OnlineResponse?> GetOnlineStatusAsync()
    {
        return await ExecuteAsync<OnlineResponse>(ONLINE_URL);
    }

    public OnlineResponse? GetOnlineStatus()
    {
        return GetOnlineStatusAsync().GetAwaiter().GetResult();
    }
    
    public async Task<AesResponse?> GetKeysAsync(string version = "")
    {
        Parameter[] parameters = !string.IsNullOrWhiteSpace(version) ? [new QueryParameter("version", version)] : [];
        return await ExecuteAsync<AesResponse>(AES_URL, parameters: parameters);
    }

    public AesResponse? GetKeys(string version = "")
    {
        return GetKeysAsync(version).GetAwaiter().GetResult();
    }

    public async Task<MappingsResponse[]?> GetMappingsAsync(string version = "")
    {
        Parameter[] parameters = !string.IsNullOrWhiteSpace(version) ? [new QueryParameter("version", version)] : [];
        return await ExecuteAsync<MappingsResponse[]>(MAPPINGS_URL, parameters: parameters);
    }

    public MappingsResponse[]? GetMappings(string version = "")
    {
        return GetMappingsAsync(version).GetAwaiter().GetResult();
    }
}