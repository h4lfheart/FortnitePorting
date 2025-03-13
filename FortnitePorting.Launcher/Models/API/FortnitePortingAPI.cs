using System.Threading.Tasks;
using FortnitePorting.Shared.Models.API;
using RestSharp;
using RepositoryResponse = FortnitePorting.Launcher.Models.API.Response.RepositoryResponse;

namespace FortnitePorting.Launcher.Models.API;

public class FortnitePortingAPI(RestClient client) : APIBase(client)
{
    public async Task<RepositoryResponse?> GetRepositoryAsync(string url)
    {
        return await ExecuteAsync<RepositoryResponse>(url);
    }

    public RepositoryResponse? GetRepository(string url)
    {
        return GetRepositoryAsync(url).GetAwaiter().GetResult();
    }
}