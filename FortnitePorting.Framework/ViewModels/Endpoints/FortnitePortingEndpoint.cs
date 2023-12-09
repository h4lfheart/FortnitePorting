using FortnitePorting.Framework.ViewModels.Endpoints.Models;
using RestSharp;

namespace FortnitePorting.Framework.ViewModels.Endpoints;

public class FortnitePortingEndpoint : EndpointBase
{
    private const string RELEASE_URL = "https://halfheart.dev/fortnite-porting/api/v2/release.json";
    private const string CHANGELOG_URL = "https://halfheart.dev/fortnite-porting/api/v2/changelog.json";
    private const string FEATURED_URL = "https://halfheart.dev/fortnite-porting/api/v2/featured.json";

    public FortnitePortingEndpoint(RestClient client) : base(client)
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

    public async Task<ChangelogResponse[]?> GetChangelogsAsync()
    {
        return await ExecuteAsync<ChangelogResponse[]>(CHANGELOG_URL);
    }

    public ChangelogResponse[]? GetChangelogs()
    {
        return GetChangelogsAsync().GetAwaiter().GetResult();
    }

    public async Task<FeaturedResponse[]?> GetFeaturedAsync()
    {
        return await ExecuteAsync<FeaturedResponse[]>(FEATURED_URL);
    }

    public FeaturedResponse[]? GetFeatured()
    {
        return GetFeaturedAsync().GetAwaiter().GetResult();
    }
}