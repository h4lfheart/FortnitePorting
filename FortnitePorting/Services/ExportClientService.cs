using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using AvaloniaEdit.Utils;
using FortnitePorting.Models.Export;
using ZstdSharp;

namespace FortnitePorting.Services;

public class ExportClientService : IService
{
    private Dictionary<EExportServerType, ExportClient> _clients = [];

    public ExportClientService()
    {
        foreach (var serverType in Enum.GetValues<EExportServerType>())
        {
            if (serverType is EExportServerType.None) continue;

            _clients[serverType] = new ExportClient(serverType);
        }
    }

    public async Task<bool> IsRunning(EExportServerType serverType)
    {
        return _clients.TryGetValue(serverType, out var client) && await client.IsRunning();
    }

    public async Task SendExportAsync<T>(EExportServerType serverType, T data)
    {
        if (_clients.TryGetValue(serverType, out var client))
        {
            await client.SendDataAsync(data, EExportCommandType.Data);
        }
    }

    public async Task SendMessageAsync(EExportServerType serverType, string message)
    {
        if (_clients.TryGetValue(serverType, out var client))
        {
            await client.SendDataAsync(message, EExportCommandType.Message);
        }
    }
}


public enum EExportServerType
{
    None = -1,
    
    [Description("Blender")]
    Blender = 40000,
    
    [Description("Unreal Engine")]
    Unreal = 40001,
    
    [Description("Unity")]
    Unity = 40002
}

public static class EExportServerTypeExtensions
{
    extension(EExportLocation exportLocation)
    {
        public EExportServerType ServerType => exportLocation switch
        {
            EExportLocation.Blender => EExportServerType.Blender,
            EExportLocation.Unreal => EExportServerType.Unreal,
            EExportLocation.Unity => EExportServerType.Unity,
            _ => EExportServerType.None
        };
    }
}