using CUE4Parse.UE4.Assets.Exports;
using FortnitePorting.Exporting.Models.Files.Meta;

namespace FortnitePorting.Exporting.Models.Files;

public class ExportFileEntry
{
    public EExportType Type { get; set; }
    public UObject Object { get; set; }
    public IExportFileMeta? Meta { get; set; }
}