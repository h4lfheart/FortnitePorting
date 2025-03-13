
using CUE4Parse.Utils;
using FortnitePorting.Shared.Framework;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace FortnitePorting.Shared.ViewModels;

public class APIViewModelBase(string userAgentVersion = "", int timeout = 10) : ViewModelBase
{
    protected readonly RestClient _client = new(new RestClientOptions
    {
        UserAgent = $"FortnitePorting/{userAgentVersion}",
        MaxTimeout = 1000 * timeout,
    }, configureSerialization: s => s.UseSerializer<JsonNetSerializer>());
    
    public string GetUrl(RestRequest request)
    {
        return _client.BuildUri(request).ToString();
    }

    public async Task<byte[]?> GetBytesAsync(string url)
    {
        var request = new RestRequest(url);
        return await _client.DownloadDataAsync(request);
    }
    
    public byte[]? GetBytes(string url)
    {
        return GetBytesAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task<FileInfo> DownloadFileAsync(string url, string destination)
    {
        var request = new RestRequest(url);
        var data = await _client.DownloadDataAsync(request);
        if (data is null) return null;

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        
        await File.WriteAllBytesAsync(destination, data);
        return new FileInfo(destination);
    }
    
    public async Task<FileInfo> DownloadFileAsync(string url, string destination, Action<float> progressAction)
    {
        using var httpClientInstance = new HttpClient(_client.Options.ConfigureMessageHandler?.Invoke(new HttpClientHandler()) ?? new HttpClientHandler());
        var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(url),
        };

        using var response = await httpClientInstance.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode) return null;

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        
        await using var responseStream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(destination, FileMode.Create, FileAccess.Write);

        var buffer = new byte[8192];
        int bytesRead;

        var totalBytesRead = 0.0f;
        var totalByteCount = response.Content.Headers.ContentLength ?? -1;
        while ((bytesRead = await responseStream.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            totalBytesRead += bytesRead;

            progressAction(totalBytesRead / totalByteCount);
        }

        
        return new FileInfo(destination);
    }

    public async Task<FileInfo> DownloadFileAsync(string url, DirectoryInfo destination)
    {
        var outPath = Path.Combine(destination.FullName, url.SubstringAfterLast("/").SubstringAfterLast("\\"));
        var request = new RestRequest(url);
        var data = await _client.DownloadDataAsync(request);
        if (data is not null) await File.WriteAllBytesAsync(outPath, data);
        return new FileInfo(outPath);
    }
    
    public FileInfo DownloadFile(string url, DirectoryInfo destination)
    {
        return DownloadFileAsync(url, destination).GetAwaiter().GetResult();
    }
}