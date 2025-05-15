using System;
using System.Threading.Tasks;
using FortnitePorting.Models.API.Base;
using FortnitePorting.Shared;
using RestSharp;

namespace FortnitePorting.Models.API;

public class FortnitePortingServerAPI(RestClient client) : APIBase(client)
{
    public async Task SendAsync(string data, EExportServerType serverType)
    {
        if (serverType == EExportServerType.None) return;
        
        var port = (int) serverType;
        var serverUrl = $"http://127.0.0.1:{port}/fortnite-porting/data";
        await ExecuteAsync(serverUrl, method: Method.Post, verbose: false, parameters: new BodyParameter(data, ContentType.Json));
    }
    
    public async Task<bool> PingAsync(EExportServerType serverType)
    {
        if (serverType == EExportServerType.None) return false;
        
        var port = (int) serverType;
        var serverUrl = $"http://127.0.0.1:{port}/fortnite-porting/ping";
        var response = await ExecuteAsync(serverUrl, method: Method.Get, verbose: false);
        return response.IsSuccessful;
    }
}

public enum EExportServerType
{
    None = -1,
    
    Blender = 20000,
    Unreal = 20001,
    Unity = 20002
}

public static class EExportServerTypeExtensions
{
    public static EExportServerType ServerType(this EExportLocation exportLocation) => exportLocation switch
    {
        EExportLocation.Blender => EExportServerType.Blender,
        EExportLocation.Unreal => EExportServerType.Unreal,
        EExportLocation.Unity => EExportServerType.Unity,
        _ => EExportServerType.None
    };
}