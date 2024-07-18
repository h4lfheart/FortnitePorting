using System.IO;
using System.Threading.Tasks;
using CUE4Parse.Utils;
using FortnitePorting.Models.API;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace FortnitePorting.ViewModels;

public class APIViewModel : ViewModelBase
{
    public readonly FortnitePortingAPI FortnitePorting;
    public readonly FortnitePortingServerAPI FortnitePortingServer;
    public readonly FortniteCentralAPI FortniteCentral;

    protected readonly RestClient _client = new(_clientOptions, configureSerialization: s => s.UseSerializer<JsonNetSerializer>());

    private static readonly RestClientOptions _clientOptions = new()
    {
        UserAgent = $"FortnitePorting/{Globals.VersionString}",
        MaxTimeout = 1000 * 10,
    };

    public APIViewModel()
    {
        FortnitePorting = new FortnitePortingAPI(_client);
        FortniteCentral = new FortniteCentralAPI(_client);
        FortnitePortingServer = new FortnitePortingServerAPI(_client);
    }

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
        if (data is not null) await File.WriteAllBytesAsync(destination, data);
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