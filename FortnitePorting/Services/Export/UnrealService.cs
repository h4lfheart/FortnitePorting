using System.Collections.Generic;
using System.Net;
using System.Text;
using FortnitePorting.Exports;
using FortnitePorting.Exports.Types;
using FortnitePorting.Exports.Unreal;
using Ionic.Zlib;
using Newtonsoft.Json;

namespace FortnitePorting.Services.Export;

public class UnrealSocketService : SocketServiceBase
{
    protected override IPEndPoint Endpoint { get; set; } = new(IPAddress.Parse(Globals.LOCALHOST), Globals.UNREAL_PORT);

    public override void Send(List<ExportDataBase> data, ExportSettingsBase settings)
    {
        var unrealExportSettings = (UnrealExportSettings)settings;
        var export = new UnrealExport
        {
            Data = data,
            Settings = unrealExportSettings,
            AssetsRoot = App.AssetsFolder.FullName.Replace("\\", "/")
        };

        var message = JsonConvert.SerializeObject(export);
        var uncompressed = Encoding.UTF8.GetBytes(message);
        // TODO GZIP COMPRESSION FOR UNREAL
        //var compressed = GZipStream.CompressBuffer(uncompressed);

        SendSpliced(uncompressed, Globals.BUFFER_SIZE);
        Client.Send(Encoding.UTF8.GetBytes(Globals.UDPClient_MessageTerminator));
    }
}

public static class UnrealService
{
    public static UnrealSocketService Client = new();
}