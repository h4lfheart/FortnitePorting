using CUE4Parse.UE4.Assets.Exports;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Models;

namespace FortnitePorting.Export.Types;

public class BaseExport
{
    public string Name;
    public EExportType Type;
    public EPrimitiveExportType PrimitiveType => Type.GetPrimitiveType();

    protected ExportContext Exporter = new();

    public BaseExport(string name, UObject asset, EExportType exportType)
    {
        Name = name;
        Type = exportType;
    }
}