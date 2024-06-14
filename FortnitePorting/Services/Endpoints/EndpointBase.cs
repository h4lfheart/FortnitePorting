using System.Threading.Tasks;
using RestSharp;

namespace FortnitePorting.Services.Endpoints;

public abstract class EndpointBase
{
    protected readonly RestClient _client;

    protected EndpointBase(RestClient client)
    {
        _client = client;
    }
    
    protected async Task<RestResponse> ExecuteAsync(string url, Method method = Method.Get, params Parameter[] parameters)
    {
        var request = new RestRequest(url, method);
        foreach (var parameter in parameters) request.AddParameter(parameter);

        var response = await _client.ExecuteAsync(request).ConfigureAwait(false);
        Log.Information("[{Method}] {StatusDescription} ({StatusCode}): {URI}", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
        if (response.ErrorException is not null) Log.Error(response.ErrorException.ToString());
        
        return response;
    }

    protected RestResponse Execute(string url, Method method = Method.Get, params Parameter[] parameters)
    {
        return ExecuteAsync(url, method, parameters).GetAwaiter().GetResult();
    }
}