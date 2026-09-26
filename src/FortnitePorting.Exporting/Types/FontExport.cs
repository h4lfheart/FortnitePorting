using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine.Font;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Exporting.Models.Files.Meta;

namespace FortnitePorting.Exporting.Types;

public class FontExport : BaseExport
{
    public string Path;
    public string? FolderPath;

    public FontExport(string name, UObject asset, EExportType exportType, ExportDataMeta metaData, IExportFileMeta? fileMeta) : base(name, exportType, metaData)
    {
        if (asset is not UFontFace fontFace) return;

        if (metaData.ExportLocation.IsFolder)
        {
            var exportPath = Context.Export(fontFace, returnRealPath: true, synchronousExport: true);
            FolderPath = System.IO.Path.GetDirectoryName(exportPath);
        }
        else
        {
            Path = Context.Export(fontFace);
        }
    }
}
