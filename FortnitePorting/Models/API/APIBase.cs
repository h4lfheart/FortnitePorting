using System;
using System.Net;
using System.Threading.Tasks;
using RestSharp;
using Serilog;

namespace FortnitePorting.Models.API;

public class APIBase
{
    protected readonly RestClient _client;

    protected APIBase(RestClient client)
    {
        _client = client;
    }

    protected async Task<T?> ExecuteAsync<T>(string url, Method method = Method.Get, string body = "", params Parameter[] parameters)
    {
        try
        {
            var request = new RestRequest(url, method);
            request.AddBody(body);
            foreach (var parameter in parameters) request.AddParameter(parameter);

            var response = await _client.ExecuteAsync<T>(request).ConfigureAwait(false);
            Log.Information("[{Method}] {StatusDescription} ({StatusCode}): {Uri}", request.Method,
                response.StatusDescription, (int) response.StatusCode, request.Resource);
            if (response.ErrorException is not null) Log.Error(response.ErrorException.ToString());
            return response.StatusCode != HttpStatusCode.OK ? default : response.Data;
        }
        catch (Exception e)
        {
            Log.Error(e.Message + e.StackTrace);
            return default;
        }
    }

    protected T? Execute<T>(string url, Method method = Method.Get, string body = "", params Parameter[] parameters)
    {
        return ExecuteAsync<T>(url, method, body, parameters).GetAwaiter().GetResult();
    }

    protected async Task<RestResponse> ExecuteAsync(string url, Method method = Method.Get, string body = "", params Parameter[] parameters)
    {
        var request = new RestRequest(url, method);
        request.AddBody(body);
        foreach (var parameter in parameters) request.AddParameter(parameter);

        var response = await _client.ExecuteAsync(request).ConfigureAwait(false);
        Log.Information("[{Method}] {StatusDescription} ({StatusCode}): {Uri}", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
        if (response.ErrorException is not null) Log.Error(response.ErrorException.ToString());
        
        return response;
    }

    protected RestResponse Execute(string url, Method method = Method.Get, string body = "", params Parameter[] parameters)
    {
        return ExecuteAsync(url, method, body, parameters).GetAwaiter().GetResult();
    }
    
}