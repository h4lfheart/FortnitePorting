using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using FortnitePorting.Application;
using FortnitePorting.Controls.Assets;
using FortnitePorting.Export;
using FortnitePorting.Export.Types;
using FortnitePorting.Extensions;
using FortnitePorting.Framework.Controls;
using FortnitePorting.Framework.Extensions;
using FortnitePorting.Framework.Services;
using Newtonsoft.Json;

namespace FortnitePorting.Services;

public static class ExportService
{
    private static readonly SocketInterface Blender = new("Blender", BLENDER_PORT, BLENDER_MESSAGE_PORT);
    private static readonly SocketInterface Unreal = new("Unreal Engine", UNREAL_PORT, UNREAL_MESSAGE_PORT);

    private const int BLENDER_PORT = 24000;
    private const int BLENDER_MESSAGE_PORT = 24001;
    private const int UNREAL_PORT = 24002;
    private const int UNREAL_MESSAGE_PORT = 24003;

    public static async Task ExportAsync(List<AssetOptions> exports, EExportTargetType exportType)
    {
        await TaskService.RunAsync(async () =>
        {
            if (exportType is EExportTargetType.Folder)
            {
                exports.ForEach(export => CreateExportData(export.AssetItem.DisplayName, export.AssetItem.Asset, export.GetSelectedStyles(), export.AssetItem.Type, exportType));
                return;
            }

            var exportService = exportType switch
            {
                EExportTargetType.Blender => Blender,
                EExportTargetType.Unreal => Unreal
            };

            if (!exportService.Ping())
            {
                var exportTypeString = exportType.GetDescription();
                MessageWindow.Show($"Failed to Connect to {exportTypeString} Server",
                    $"Please ensure that you have {exportTypeString} open with the latest FortnitePorting plugin enabled.");
                return;
            }

            var exportDatas = exports.Select(export => CreateExportData(export.AssetItem.DisplayName, export.AssetItem.Asset, export.GetSelectedStyles(), export.AssetItem.Type, exportType)).ToArray();
            foreach (var exportData in exportDatas) exportData.WaitForExports();

            var exportResponse = CreateExportResponse(exportDatas, exportType);
            exportService.SendExport(JsonConvert.SerializeObject(exportResponse));
        });
    }

    public static async Task ExportAsync(List<KeyValuePair<UObject, EAssetType>> assets, EExportTargetType exportType)
    {
        await TaskService.RunAsync(async () =>
        {
            if (exportType is EExportTargetType.Folder)
            {
                assets.ForEach(kvp => CreateExportData(kvp.Key.Name, kvp.Key, Array.Empty<FStructFallback>(), kvp.Value, exportType));
                return;
            }

            var exportService = exportType switch
            {
                EExportTargetType.Blender => Blender,
                EExportTargetType.Unreal => Unreal
            };

            if (!exportService.Ping())
            {
                var exportTypeString = exportType.GetDescription();
                MessageWindow.Show($"Failed to Connect to {exportTypeString} Server",
                    $"Please ensure that you have {exportTypeString} open with the latest FortnitePorting plugin enabled.");
                return;
            }

            var exportDatas = assets.Select(kvp => CreateExportData(kvp.Key.Name, kvp.Key, Array.Empty<FStructFallback>(), kvp.Value, exportType)).ToArray();
            foreach (var exportData in exportDatas) exportData.WaitForExports();

            var exportResponse = CreateExportResponse(exportDatas, exportType);
            exportService.SendExport(JsonConvert.SerializeObject(exportResponse));
        });
    }

    private static ExportResponse CreateExportResponse(ExportDataBase[] exportData, EExportTargetType exportType)
    {
        return new ExportResponse
        {
            AssetsFolder = AppSettings.Current.GetExportPath(),
            Options = AppSettings.Current.ExportOptions.Get(exportType),
            Data = exportData
        };
    }

    private static ExportDataBase CreateExportData(string name, UObject asset, FStructFallback[] styles, EAssetType assetType, EExportTargetType exportType)
    {
        return assetType.GetExportType() switch
        {
            EExportType.Mesh => new MeshExportData(name, asset, styles, assetType, exportType),
            EExportType.Animation => new AnimExportData(name, asset, styles, assetType, exportType),
            EExportType.Texture => new TextureExportData(name, asset, styles, assetType, exportType),
            _ => throw new ArgumentOutOfRangeException(assetType.ToString())
        };
    }
}

public class SocketInterfaceBase
{
    protected UdpClient Client;
    private IPEndPoint EndPoint;

    public SocketInterfaceBase(int port, bool isServer = false)
    {
        EndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);

        if (isServer)
        {
            Client = new UdpClient(EndPoint);
        }
        else
        {
            Client = new UdpClient();
            Client.Connect(EndPoint);
        }
    }
    
    public int SendData(string data)
    {
        try
        {
            return Client.Send(data.StringToBytes());
        }
        catch (SocketException)
        {
            Reconnect();
            return 0;
        }
    }

    public string ReceiveData()
    {
        try
        {
            return Client.Receive(ref EndPoint).BytesToString();
        }
        catch (SocketException)
        {
            Reconnect();
            return string.Empty;
        }
    }

    private void Reconnect()
    {
        Client.Close();
        Client = new UdpClient();
        Client.Connect(EndPoint);
    }
}

public class SocketInterface : SocketInterfaceBase
{
    private readonly SocketInterfaceBase MessageServer;

    private const string COMMAND_START = "Start";
    private const string COMMAND_STOP = "Stop";
    private const string COMMAND_PING_REQUEST = "Ping";
    private const string COMMAND_PING_RESPONSE = "Pong";
    private const int BUFFER_SIZE = 1024;

    public SocketInterface(string name, int port, int messagePort) : base(port)
    {
        MessageServer = new SocketInterfaceBase(messagePort, isServer: true);
        TaskService.Run(() =>
        {
            while (true)
            {
                var response = MessageServer.ReceiveData();
                MessageWindow.Show($"Message from {name} Server", response);
            }
        });
    }

    public void SendExport(string export)
    {
        SendData(COMMAND_START);
        SendChunked(export);
        SendData(COMMAND_STOP);
    }

    public bool Ping()
    {
        var bytesSent = SendData(COMMAND_PING_REQUEST);
        if (bytesSent <= 0) return false;
        
        return ReceiveData().Equals(COMMAND_PING_RESPONSE);
    }

    private int SendChunked(string data)
    {
        var totalBytesSent = 0;
        var chunks = data.StringToBytes().Chunk(BUFFER_SIZE);
        foreach (var (index, chunk) in chunks.Enumerate())
        {
            var sendAttempts = 0;
            while (true)
            {
                sendAttempts++;
                if (sendAttempts > 10) throw new Exception($"Failed to send chunk {index}, export cannot continue");

                var bytesSent = Client.Send(chunk);
                if (bytesSent <= 0) continue;
                
                if (Ping()) break;
            }

            totalBytesSent += chunk.Length;
        }

        return totalBytesSent;
    }
    
}