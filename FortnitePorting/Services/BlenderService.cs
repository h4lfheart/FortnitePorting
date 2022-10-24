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
using Newtonsoft.Json;

namespace FortnitePorting.Services;

public static class BlenderService
{
    private static readonly UdpClient Client = new();

    static BlenderService()
    {
        Client.Connect("localhost", Globals.BLENDER_PORT);
    }

    public static async Task Send(ExportData data, BlenderExportSettings settings)
    {
        var export = new BlenderExport
        {
            Data = data,
            Settings = settings,
            AssetsRoot = App.AssetsFolder.FullName.Replace("\\", "/")
        };

        var message = JsonConvert.SerializeObject(export);
        var messageBytes = Encoding.ASCII.GetBytes(message);

        Client.SendSpliced(messageBytes, Globals.BUFFER_SIZE);
        Client.Send(Encoding.ASCII.GetBytes("FPMessageFinished"));
    }

    public static int SendSpliced(this UdpClient client, IEnumerable<byte> arr, int size)
    {
        return arr.Chunk(size).ToList().Sum(chunk => client.Send(chunk));
    }
    
}