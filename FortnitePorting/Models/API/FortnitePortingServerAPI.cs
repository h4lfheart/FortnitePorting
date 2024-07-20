using System.Threading.Tasks;
using RestSharp;

namespace FortnitePorting.Models.API;

public class FortnitePortingServerAPI : APIBase
{
    public FortnitePortingServerAPI(RestClient client) : base(client)
    {
    }

    public async Task SendAsync(string data, EExportServerType serverType)
    {
        var port = (int) serverType;
        var serverUrl = $"http://127.0.0.1:{port}/fortnite-porting/data";
        await ExecuteAsync(serverUrl, method: Method.Post, verbose: false, parameters: new BodyParameter(data, ContentType.Json));
    }
    
    public async Task<bool> PingAsync(EExportServerType serverType)
    {
        var port = (int) serverType;
        var serverUrl = $"http://127.0.0.1:{port}/fortnite-porting/ping";
        var response = await ExecuteAsync(serverUrl, method: Method.Get, verbose: false);
        return response.IsSuccessful;
    }
}

public enum EExportServerType
{
    Blender = 20000,
    Unreal = 20001,
    Unity = 20002
}