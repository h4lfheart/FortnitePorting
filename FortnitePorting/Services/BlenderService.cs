using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using FortnitePorting.Exports.Blender;
using FortnitePorting.Exports.Types;
using FortnitePorting.Views.Extensions;
using Ionic.Zlib;
using Newtonsoft.Json;

namespace FortnitePorting.Services;

public static class BlenderService
{
    private static UdpClient Client = new();
    private static readonly IPEndPoint Endpoint = IPEndPoint.Parse(Globals.LOCALHOST + ":" + Globals.BLENDER_PORT);

    static BlenderService()
    {
        Client.Connect(Endpoint);
    }

    public static void Send(List<ExportDataBase> data, BlenderExportSettings settings)
    {
        var export = new BlenderExport
        {
            Data = data,
            Settings = settings,
            AssetsRoot = App.AssetsFolder.FullName.Replace("\\", "/")
        };

        var message = JsonConvert.SerializeObject(export);
        var uncompressed = Encoding.UTF8.GetBytes(message);
        var compressed = GZipStream.CompressBuffer(uncompressed);
        
        Client.SendSpliced(compressed, Globals.BUFFER_SIZE);
        Client.Send(Encoding.UTF8.GetBytes(Globals.UDPClient_MessageTerminator));
    }

    public static bool PingServer()
    {
        Client.Send(Encoding.UTF8.GetBytes(Globals.UDPClient_Ping));
        if (Client.TryReceive(Endpoint, out var response))
        {
            var responseString = Encoding.UTF8.GetString(response);
            return responseString.Equals(Globals.UDPClient_Ping);
        }

        return false;
    }
    
    public static bool ReceivePing()
    {
        if (Client.TryReceive(Endpoint, out var response))
        {
            var responseString = Encoding.UTF8.GetString(response);
            return responseString.Equals(Globals.UDPClient_Ping);
        }

        return false;
    }

    public static int SendSpliced(this UdpClient client, IEnumerable<byte> arr, int size)
    {
        var chunks = arr.Chunk(size).ToList();

        var dataSent = 0;
        foreach (var (index, chunk) in chunks.Enumerate())
        {
            var chunkSize = Client.Send(chunk);
            while (!ReceivePing())
            {
                Log.Warning("Lost Chunk {Index}, Retrying...", index);
                chunkSize = Client.Send(chunk);
            }

            dataSent += chunkSize;
        }

        return dataSent;
    }

    public static bool TryReceive(this UdpClient client, IPEndPoint endpoint, out byte[] data)
    {
        data = Array.Empty<byte>();
        try
        {
            data = Task.Run(() => client.Receive(ref endpoint)).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        catch (SocketException)
        {
            Client.Close();
            Client = new UdpClient();
            Client.Connect(endpoint);
            return false;
        }

        return true;
    }
}