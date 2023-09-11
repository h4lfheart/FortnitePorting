using System.Threading.Tasks;
using FortnitePorting.Services.Endpoints.Models;
using RestSharp;
using Serilog;

namespace FortnitePorting.Services.Endpoints;

public abstract class EndpointBase
{
    protected readonly RestClient _client;

    protected EndpointBase(RestClient client)
    {
        _client = client;
    }

    protected async Task<T?> GetAsync<T>(string url)
    {
        var request = new RestRequest(url);
        var response = await _client.ExecuteAsync<T>(request).ConfigureAwait(false);
        Log.Information("[{Method}] {StatusDescription} ({StatusCode}): {URI}", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
        return response.Data;
    }
    
    protected T? Get<T>(string url)
    {
        return GetAsync<T>(url).GetAwaiter().GetResult();
    }
}