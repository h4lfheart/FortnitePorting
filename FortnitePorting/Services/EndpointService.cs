using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.Utils;
using FortnitePorting.Services.Endpoints;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace FortnitePorting.Services;

public static class EndpointService
{
    private static readonly RestClient _client = new(new RestClientOptions()
    {
        UserAgent = $"FortnitePorting/{Globals.VERSION}",
        MaxTimeout = Timeout.Infinite
    }, configureSerialization: s => s.UseSerializer<JsonNetSerializer>());

    public static readonly FortniteCentralEndpoint FortniteCentral = new(_client);
    public static readonly FortnitePortingEndpoint FortnitePorting = new(_client);
    public static readonly EpicGamesEndpoint EpicGames = new(_client);
    public static readonly BlenderEndpoint Blender = new(_client);
    
    public static byte[] DownloadFile(string url)
    {
        return DownloadFileAsync(url).GetAwaiter().GetResult();
    }

    public static async Task<byte[]> DownloadFileAsync(string url)
    {
        var request = new RestRequest(url);
        var data = await _client.DownloadDataAsync(request);
        return data;
    }

    public static FileInfo DownloadFile(string url, string destination)
    {
        return DownloadFileAsync(url, destination).GetAwaiter().GetResult();
    }

    public static async Task<FileInfo> DownloadFileAsync(string url, string destination)
    {
        var request = new RestRequest(url);
        var data = await _client.DownloadDataAsync(request);
        if (data is not null) await File.WriteAllBytesAsync(destination, data);
        return new FileInfo(destination);
    }
    
    public static FileInfo DownloadFile(string url, DirectoryInfo destination)
    {
        return DownloadFileAsync(url, destination).GetAwaiter().GetResult();
    }

    public static async Task<FileInfo> DownloadFileAsync(string url, DirectoryInfo destination)
    {
        var outPath = Path.Combine(destination.FullName, url.SubstringAfterLast("/").SubstringAfterLast("\\"));
        var request = new RestRequest(url);
        var data = await _client.DownloadDataAsync(request);
        if (data is not null) await File.WriteAllBytesAsync(outPath, data);
        return new FileInfo(outPath);
    }
}