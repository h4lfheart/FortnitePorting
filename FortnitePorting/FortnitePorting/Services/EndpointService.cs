using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
}