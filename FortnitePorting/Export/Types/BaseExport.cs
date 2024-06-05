using CUE4Parse.UE4.Assets.Exports;
using FortnitePorting.Models.CUE4Parse;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Models;
using FortnitePorting.Shared.Models.CUE4Parse;

namespace FortnitePorting.Export.Types;

public class BaseExport
{
    public string Name;
    public EExportType Type;
    public EPrimitiveExportType PrimitiveType => Type.GetPrimitiveType();

    protected ExportContext Exporter;

    public BaseExport(string name, UObject asset, EExportType exportType, ExportMetaData metaData)
    {
        Name = name;
        Type = exportType;

        Exporter = new ExportContext(asset.Owner?.Provider as HybridFileProvider, metaData);
    }
}