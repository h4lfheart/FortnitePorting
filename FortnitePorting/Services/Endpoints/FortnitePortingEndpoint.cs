using System.Threading.Tasks;
using DynamicData;
using FortnitePorting.Services.Endpoints.Models;
using RestSharp;

namespace FortnitePorting.Services.Endpoints;

public class FortnitePortingEndpoint : EndpointBase
{
    private const string CHANGELOG_URL = "https://halfheart.dev/fortnite-porting/api/v2/changelog.json";
    
    public FortnitePortingEndpoint(RestClient client) : base(client)
    {
    }

    public async Task<ChangelogResponse[]?> GetChangelogsAsync() => await ExecuteAsync<ChangelogResponse[]>(CHANGELOG_URL);
    public ChangelogResponse[]? GetChangelogs() => GetChangelogsAsync().GetAwaiter().GetResult();
}