using System.Threading.Tasks;
using FortnitePorting.Shared.Models.API;
using FortnitePorting.Shared.Models.API.Responses;
using RestSharp;

namespace FortnitePorting.Installer.Models.API;

public class FortnitePortingAPI : APIBase
{
    public const string RELEASE_URL = "https://fortniteporting.halfheart.dev/api/v3/release";

    
    public FortnitePortingAPI(RestClient client) : base(client)
    {
    }
    
    
    public async Task<ReleaseResponse?> GetReleaseAsync()
    {
        return await ExecuteAsync<ReleaseResponse>(RELEASE_URL);
    }

    public ReleaseResponse? GetRelease()
    {
        return GetReleaseAsync().GetAwaiter().GetResult();
    }
}