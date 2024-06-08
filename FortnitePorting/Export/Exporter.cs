using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Export.Types;
using FortnitePorting.Models.API;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Models;
using FortnitePorting.Shared.Services;
using Newtonsoft.Json;

namespace FortnitePorting.Export;

public static class Exporter
{
    public static async Task Export(string name, UObject asset, FStructFallback[] styles, EExportType exportType, ExportMetaData metaData)
    {
        await TaskService.RunAsync(async () =>
        {
            if (await ApiVM.FortnitePortingServer.PingAsync(EExportServerType.Blender) is false)
            {
                AppVM.Message("Blender Server", "The blender server for Fortnite Porting is not currently running.", InfoBarSeverity.Error, false);
                return;
            }
            
            var exportData = new ExportData
            {
                MetaData = metaData,
                Exports = exportType.GetPrimitiveType() switch
                {
                    EPrimitiveExportType.Mesh => [new MeshExport(name, asset, styles, exportType, metaData)],
                    EPrimitiveExportType.Animation => [],
                    EPrimitiveExportType.Texture => [],
                    EPrimitiveExportType.Sound => []
                }
            };

            var data = JsonConvert.SerializeObject(exportData);
            await ApiVM.FortnitePortingServer.SendAsync(data, EExportServerType.Blender);
        });
    }
}