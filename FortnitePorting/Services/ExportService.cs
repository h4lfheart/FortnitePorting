using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using FortnitePorting.Application;
using FortnitePorting.Controls.Assets;
using FortnitePorting.Export;
using FortnitePorting.Export.Types;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels;
using Newtonsoft.Json;
using Serilog;

namespace FortnitePorting.Services;

public static class ExportService
{
    public static SocketInterface Blender = new(BLENDER_PORT);
    public static SocketInterface Unreal = new(UNREAL_PORT);
    
    public const int BLENDER_PORT = 24000;
    public const int UNREAL_PORT = 24001;
    
    private static EAssetType[] MeshTypes =
    {
        EAssetType.Outfit,
        EAssetType.Backpack,
        EAssetType.Pickaxe,
        EAssetType.Glider,
        EAssetType.Pet,
        EAssetType.Toy,
        EAssetType.Prop,
        EAssetType.Gallery,
        EAssetType.Item,
        EAssetType.Trap,
        EAssetType.Vehicle,
        EAssetType.Wildlife,
        EAssetType.Mesh
    };
    
    private static EAssetType[] AnimTypes =
    {
        EAssetType.Emote
    };
    
    private static EAssetType[] TextureTypes =
    {
        EAssetType.Spray,
        EAssetType.Banner,
        EAssetType.LoadingScreen
    };

    public static async Task ExportAsync(AssetItem asset, EExportType exportType)
    {
        await TaskService.RunAsync(async () =>
        {
            if (exportType is EExportType.Folder)
            {
                CreateExportData(asset.DisplayName, asset.Asset, asset.Type, exportType);
                return;
            }

            var exportService = exportType switch
            {
                EExportType.Blender => Blender,
                EExportType.Unreal => Unreal
            };

            if (!exportService.Ping())
            {
                var exportTypeString = exportType.GetDescription();
                MessageWindow.Show($"Failed to Connect to {exportTypeString} Server",
                    $"Please ensure that you have {exportTypeString} open with the latest FortnitePorting plugin enabled.");
                return;
            }

            var exportData = CreateExportData(asset.DisplayName, asset.Asset, asset.Type, exportType);
            await exportData.WaitForExportsAsync();

            var exportResponse = CreateExportResponse(exportData);
            exportService.SendMessage(JsonConvert.SerializeObject(exportResponse));
        });
    }
    
    public static async Task ExportAsync(UObject asset, EAssetType assetType, EExportType exportType)
    {
        await TaskService.RunAsync(async () =>
        {
            if (exportType is EExportType.Folder)
            {
                CreateExportData(asset.Name, asset, assetType, exportType);
                return;
            }

            var exportService = exportType switch
            {
                EExportType.Blender => Blender,
                EExportType.Unreal => Unreal
            };

            if (!exportService.Ping())
            {
                var exportTypeString = exportType.GetDescription();
                MessageWindow.Show($"Failed to Connect to {exportTypeString} Server",
                    $"Please ensure that you have {exportTypeString} open with the latest FortnitePorting plugin enabled.");
                return;
            }

            var exportData = CreateExportData(asset.Name, asset, assetType, exportType);
            await exportData.WaitForExportsAsync();

            var exportResponse = CreateExportResponse(exportData);
            exportService.SendMessage(JsonConvert.SerializeObject(exportResponse));
        });
    }

    private static ExportResponse CreateExportResponse(ExportDataBase exportData)
    {
        return new ExportResponse
        {
            AssetsFolder = AppSettings.Current.ExportPath,
            Options = exportData.ExportOptions,
            Data = exportData
        };
    }

    private static ExportDataBase CreateExportData(string name, UObject asset, EAssetType assetType, EExportType exportType)
    {
        if (MeshTypes.Contains(assetType))
        {
            return new MeshExportData(name, asset, assetType, exportType);
        }

        if (AnimTypes.Contains(assetType))
        {
            return new AnimExportData(name, asset, assetType, exportType);
        }

        if (TextureTypes.Contains(assetType))
        {
            return new TextureExportData(name, asset, assetType, exportType);
        }

        return null!; // never reached idc
    }
}

public class SocketInterface
{
    private IPEndPoint EndPoint;
    private UdpClient Client = new();
    
    private const string COMMAND_START = "Start";
    private const string COMMAND_STOP = "Stop";
    private const string COMMAND_PING_REQUEST = "Ping";
    private const string COMMAND_PING_RESPONSE = "Pong";
    private const int BUFFER_SIZE = 1024;

    public SocketInterface(int port)
    {
        EndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
        Client.Connect(EndPoint);
    }

    public void SendMessage(string str)
    {
        Client.Send(COMMAND_START.Bytes());
        SendSpliced(str.Bytes(), BUFFER_SIZE);
        Client.Send(COMMAND_STOP.Bytes());
    }

    public bool Ping()
    {
        Client.Send(COMMAND_PING_REQUEST.Bytes());
        return TryReceive(out var response) && response.String().Equals(COMMAND_PING_RESPONSE);
    }
    
    public int SendSpliced(IEnumerable<byte> arr, int size)
    {
        var chunks = arr.Chunk(size).ToList();

        var dataSent = 0;
        foreach (var (index, chunk) in chunks.Enumerate())
        {
            var chunkSize = Client.Send(chunk);
            while (!Ping())
            {
                Log.Warning("Lost Chunk {Index}, Retrying...", index);
                chunkSize = Client.Send(chunk);
            }

            dataSent += chunkSize;
        }

        return dataSent;
    }
    
    private bool TryReceive(out byte[] data)
    {
        data = Array.Empty<byte>();
        try
        {
            data = Client.Receive(ref EndPoint);
        }
        catch (SocketException)
        {
            Client.Close();
            Client = new UdpClient();
            Client.Connect(EndPoint);
            return false;
        }

        return true;
    }
}