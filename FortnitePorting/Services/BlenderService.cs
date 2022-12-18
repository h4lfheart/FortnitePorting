using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AdonisUI.Controls;
using FortnitePorting.Exports;
using FortnitePorting.Exports.Blender;
using FortnitePorting.Exports.Types;
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
        var messageBytes = Encoding.UTF8.GetBytes(message);

        Client.SendSpliced(messageBytes, Globals.BUFFER_SIZE);
        Client.Send(Encoding.UTF8.GetBytes(Globals.UDPClient_MessageTerminator));
        
        if (Client.TryReceive(Endpoint, out var response))
        {
            var responseString = Encoding.UTF8.GetString(response);
        }
        else
        {
            AppVM.Warning("Failed to Establish Connection with FortnitePorting Server", "Please make sure you have installed the FortnitePortingServer.zip file and have an instance of Blender open.");
        }
    }

    public static bool IsServerRunning()
    {
        Client.Send(Encoding.UTF8.GetBytes(Globals.UDPClient_ServerCheck));
        if (Client.TryReceive(Endpoint, out var response))
        {
            var responseString = Encoding.UTF8.GetString(response);
            return responseString.Equals(Globals.UDPServer_ResponseReceived);
        }

        return false;
    }

    public static int SendSpliced(this UdpClient client, IEnumerable<byte> arr, int size)
    {
        return arr.Chunk(size).ToList().Sum(chunk => client.Send(chunk));
    }

    public static bool TryReceive(this UdpClient client, IPEndPoint endpoint, out byte[] data)
    {
        data = Array.Empty<byte>();
        try
        {
            data = client.Receive(ref endpoint);
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