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
    
    public async Task<byte[]?> DownloadFileAsync(string url)
    {
        var request = new RestRequest(url);
        var data = await _client.DownloadDataAsync(request);
        return data;
    }
    
    public byte[]? DownloadFile(string url)
    {
        return DownloadFileAsync(url).GetAwaiter().GetResult();
    }

    public async Task<FileInfo> DownloadFileAsync(string url, string destination)
    {
        var request = new RestRequest(url);
        var data = await _client.DownloadDataAsync(request);
        if (data is null) return null;
        
        await File.WriteAllBytesAsync(destination, data);
        return new FileInfo(destination);
    }
    
    public FileInfo DownloadFile(string url, string destination)
    {
        return DownloadFileAsync(url, destination).GetAwaiter().GetResult();
    }
    
    public FileInfo DownloadFile(string url, DirectoryInfo destination)
    {
        return DownloadFileAsync(url, destination).GetAwaiter().GetResult();
    }

    public async Task<FileInfo> DownloadFileAsync(string url, DirectoryInfo destination)
    {
        var outPath = Path.Combine(destination.FullName, url.SubstringAfterLast("/").SubstringAfterLast("\\"));
        var request = new RestRequest(url);
        var data = await _client.DownloadDataAsync(request);
        if (data is not null) await File.WriteAllBytesAsync(outPath, data);
        return new FileInfo(outPath);
    }
}