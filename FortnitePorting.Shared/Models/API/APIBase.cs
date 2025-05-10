using System.Net;
using RestSharp;
using Serilog;

namespace FortnitePorting.Shared.Models.API;

public class APIBase
{
    protected virtual string BaseURL => string.Empty;

    protected readonly RestClient _client;

    protected APIBase(RestClient client)
    {
        _client = client;
    }

    protected async Task<T?> ExecuteAsync<T>(string url, Method method = Method.Get, bool verbose = true, params Parameter[] parameters)
    {
        try
        {
            var request = new RestRequest(string.IsNullOrEmpty(BaseURL) ? url : $"{BaseURL}/{url}", method);
            foreach (var parameter in parameters) request.AddParameter(parameter);

            var response = await _client.ExecuteAsync<T>(request).ConfigureAwait(false);
            if (verbose) Log.Information("[{Method}] {StatusDescription} ({StatusCode}): {Uri}", request.Method,
                response.StatusDescription, (int) response.StatusCode, request.Resource);
            if (verbose && response.ErrorException is not null) Log.Error(response.ErrorException.ToString());
            return response.StatusCode != HttpStatusCode.OK ? default : response.Data;
        }
        catch (Exception e)
        {
            Log.Error(e.Message + e.StackTrace);
            return default;
        }
    }

    protected T? Execute<T>(string url, Method method = Method.Get, bool verbose = true,params Parameter[] parameters)
    {
        return ExecuteAsync<T>(url, method, verbose, parameters).GetAwaiter().GetResult();
    }

    protected async Task<RestResponse> ExecuteAsync(string url, Method method = Method.Get, bool verbose = true, ApiFile[]? files = null, params Parameter[] parameters)
    {
        files ??= [];
        
        var request = new RestRequest(string.IsNullOrEmpty(BaseURL) ? url : $"{BaseURL}/{url}", method);
        foreach (var parameter in parameters) request.AddParameter(parameter);
        foreach (var file in files) request.AddFile(file.PropertyName, file.Data, file.Name);

        var response = await _client.ExecuteAsync(request).ConfigureAwait(false);
        if (verbose) Log.Information("[{Method}] {StatusDescription} ({StatusCode}): {Uri}", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
        if (verbose && response.ErrorException is not null) Log.Error(response.ErrorException.ToString());
        if (verbose && response.StatusCode != HttpStatusCode.OK) Log.Error(response.Content);
        
        return response;
    }

    protected RestResponse Execute(string url, Method method = Method.Get,bool verbose = true, params Parameter[] parameters)
    {
        return ExecuteAsync(url, method, verbose, null, parameters).GetAwaiter().GetResult();
    }
    
}