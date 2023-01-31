using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using FortnitePorting.Exports;
using FortnitePorting.Exports.Blender;
using FortnitePorting.Exports.Types;
using FortnitePorting.Views.Extensions;
using Newtonsoft.Json;

namespace FortnitePorting.Services.Export;

public abstract class SocketServiceBase
{
    protected virtual IPEndPoint Endpoint { get; set; }
    protected UdpClient Client = new();

    public SocketServiceBase()
    {
        Client.Connect(Endpoint);
    }

    public virtual void Send(List<ExportDataBase> data, ExportSettingsBase settings)
    {
        
    }

    public bool PingServer()
    {
        Client.Send(Encoding.UTF8.GetBytes(Globals.UDPClient_Ping));
        return ReceivePing();
    }

    private bool ReceivePing()
    {
        if (TryReceive(Endpoint, out var response))
        {
            var responseString = Encoding.UTF8.GetString(response);
            return responseString.Equals(Globals.UDPClient_Ping);
        }

        return false;
    }
    
    public int SendSpliced(IEnumerable<byte> arr, int size)
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

    private bool TryReceive(IPEndPoint endpoint, out byte[] data)
    {
        data = Array.Empty<byte>();
        try
        {
            data = Task.Run(() => Client.Receive(ref endpoint)).ConfigureAwait(false).GetAwaiter().GetResult();
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

