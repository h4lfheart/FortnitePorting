using System.Collections.Generic;
using System.Net;
using System.Text;
using FortnitePorting.Exports;
using FortnitePorting.Exports.Blender;
using FortnitePorting.Exports.Types;
using Ionic.Zlib;
using Newtonsoft.Json;

namespace FortnitePorting.Services.Export;

public class BlenderSocketService : SocketServiceBase
{
    protected override IPEndPoint Endpoint { get; set; } = new(IPAddress.Parse(Globals.LOCALHOST), Globals.BLENDER_PORT);

    public override void Send(List<ExportDataBase> data, ExportSettingsBase settings)
    {
        var blenderExportSettings = (BlenderExportSettings)settings;
        var export = new BlenderExport
        {
            Data = data,
            Settings = blenderExportSettings,
            AssetsRoot = App.AssetsFolder.FullName.Replace("\\", "/")
        };

        var message = JsonConvert.SerializeObject(export);
        var uncompressed = Encoding.UTF8.GetBytes(message);
        var compressed = GZipStream.CompressBuffer(uncompressed);

        SendSpliced(compressed, Globals.BUFFER_SIZE);
        Client.Send(Encoding.UTF8.GetBytes(Globals.UDPClient_MessageTerminator));
    }
}

public static class BlenderService
{
    public static BlenderSocketService Client = new();
}