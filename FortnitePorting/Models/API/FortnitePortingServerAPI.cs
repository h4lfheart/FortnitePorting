using System.Threading.Tasks;
using FortnitePorting.Models.API.Responses;
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
        await ExecuteAsync(serverUrl, body: data, method: Method.Post);
    }
}

public enum EExportServerType
{
    Blender = 20000,
    Unreal = 20001,
    Unity = 20002
}