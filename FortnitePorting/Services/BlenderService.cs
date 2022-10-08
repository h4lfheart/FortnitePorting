using System;
using System.Net.Sockets;
using System.Text;
using FortnitePorting.Export;
using FortnitePorting.Export.Blender;
using Newtonsoft.Json;

namespace FortnitePorting.Services;

public static class BlenderService
{
    private static UdpClient Client = new();

    static BlenderService()
    {
        Client.Connect("localhost", Globals.BLENDER_PORT);
    }

    public static void Send(ExportData data, BlenderExportSettings settings)
    {
        var export = new BlenderExport
        {
            Data = data,
            Settings = settings
        };
        
        Console.WriteLine(JsonConvert.SerializeObject(export));
        Client.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(export)));
    }
}