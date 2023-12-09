using System;
using System.Net;
using System.Threading.Tasks;
using RestSharp;
using Serilog;

namespace FortnitePorting.Framework.ViewModels.Endpoints;

public abstract class EndpointBase
{
    protected readonly RestClient _client;

    protected EndpointBase(RestClient client)
    {
        _client = client;
    }

    protected async Task<T?> ExecuteAsync<T>(string url, Method method = Method.Get, params Parameter[] parameters)
    {
        try
        {
            var request = new RestRequest(url, method);
            foreach (var parameter in parameters) request.AddParameter(parameter);

            var response = await _client.ExecuteAsync<T>(request).ConfigureAwait(false);
            Log.Information("[{Method}] {StatusDescription} ({StatusCode}): {Uri}", request.Method,
                response.StatusDescription, (int) response.StatusCode, request.Resource);
            return response.StatusCode != HttpStatusCode.OK ? default : response.Data;
        }
        catch (Exception e)
        {
            Log.Error(e.Message + e.StackTrace);
            return default;
        }
    }

    protected T? Execute<T>(string url, Method method = Method.Get, params Parameter[] parameters)
    {
        return ExecuteAsync<T>(url, method, parameters).GetAwaiter().GetResult();
    }

    protected async Task<RestResponse> ExecuteAsync(string url, Method method = Method.Get, params Parameter[] parameters)
    {
        var request = new RestRequest(url, method);
        foreach (var parameter in parameters) request.AddParameter(parameter);

        var response = await _client.ExecuteAsync(request).ConfigureAwait(false);
        Log.Information("[{Method}] {StatusDescription} ({StatusCode}): {Uri}", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
        return response;
    }

    protected RestResponse Execute(string url, Method method = Method.Get, params Parameter[] parameters)
    {
        return ExecuteAsync(url, method, parameters).GetAwaiter().GetResult();
    }
}