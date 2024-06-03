using CUE4Parse.UE4.Assets.Exports;
using FortnitePorting.Export.Types;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Models;
using Newtonsoft.Json;

namespace FortnitePorting.Export;

public static class Exporter
{
    public static async Task<string> Export(string name, UObject asset, EExportType exportType)
    {
        var exportData = new ExportData();
        exportData.MetaData = new ExportMetaData();
        exportData.Exports = exportType.GetPrimitiveType() switch
        {
            EPrimitiveExportType.Mesh => [new MeshExport(name, asset, exportType)],
            EPrimitiveExportType.Animation => [],
            EPrimitiveExportType.Texture => [],
            EPrimitiveExportType.Sound => [],
            _ => throw new ArgumentOutOfRangeException()
        };
        
        return JsonConvert.SerializeObject(exportData);
    }
}