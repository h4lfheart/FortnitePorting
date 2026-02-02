using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine.Font;
using FortnitePorting.Exporting.Models;

namespace FortnitePorting.Exporting.Types;

public class FontExport : BaseExport
{
    public string Path;
    
    public FontExport(string name, UObject asset, EExportType exportType, ExportDataMeta metaData) : base(name, exportType, metaData)
    {
        if (asset is not UFontFace fontFace) return;

        if (metaData.ExportLocation.IsFolder)
        {
            var exportPath = Exporter.Export(fontFace, returnRealPath: true, synchronousExport: true);
            App.Launch(System.IO.Path.GetDirectoryName(exportPath)!);
        }
        else
        {
            Path = Exporter.Export(fontFace);
        }
    }
    
}