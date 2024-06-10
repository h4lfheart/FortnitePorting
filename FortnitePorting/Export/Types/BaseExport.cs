using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using FortnitePorting.Export.Models;
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

    public BaseExport(string name, UObject asset, FStructFallback[] styles, EExportType exportType, ExportDataMeta metaData)
    {
        Name = name;
        Type = exportType;

        Exporter = new ExportContext(metaData);
    }
    
    public void WaitForExports()
    {
        foreach (var task in Exporter.ExportTasks)
        {
            task.Wait();
        }
    }
}