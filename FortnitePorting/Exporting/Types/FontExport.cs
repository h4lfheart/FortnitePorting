using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine.Font;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Models.Assets;

namespace FortnitePorting.Exporting.Types;

public class FontExport : BaseExport
{
    public string Path;
    
    public FontExport(string name, UObject asset, BaseStyleData[] styles, EExportType exportType, ExportDataMeta metaData) : base(name, asset, styles, exportType, metaData)
    {
        if (asset is not UFontFace fontFace) return;

        if (metaData.ExportLocation.IsFolder())
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