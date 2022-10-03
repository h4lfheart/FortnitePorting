using System.IO;
using System.Threading.Tasks;
using FortnitePorting.Services.Endpoints;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace FortnitePorting.Services;

public static class EndpointService
{
    private static readonly RestClient _client = new RestClient
    {
        Options =
        {
            UserAgent = "FortnitePorting",
            MaxTimeout = 3000
        }
    }.UseSerializer<JsonNetSerializer>();

    public static readonly FortniteCentralEndpoint FortniteCentral = new(_client);
    
    public static void DownloadFile(string url, string destination)
    {
        DownloadFileAsync(url, destination).GetAwaiter().GetResult();
    }
    
    public static async Task DownloadFileAsync(string url, string destination)
    {
        var request = new RestRequest(url);
        var data = await _client.DownloadDataAsync(request);
        if (data is not null) await File.WriteAllBytesAsync(destination, data);
    }
}