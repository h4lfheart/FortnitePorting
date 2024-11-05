using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using FortnitePorting.Export.Models;
using FortnitePorting.Models;
using FortnitePorting.Models.Assets;
using FortnitePorting.Models.CUE4Parse;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Models;


namespace FortnitePorting.Export.Types;

public class BaseExport
{
    public string Name;
    public EExportType Type;
    public EPrimitiveExportType PrimitiveType => Type.GetPrimitiveType();

    protected ExportContext Exporter;

    public BaseExport(string name, UObject asset, BaseStyleData[] styles, EExportType exportType, ExportDataMeta metaData)
    {
        Name = name;
        Type = exportType;

        Exporter = new ExportContext(metaData);
    }
    
    public BaseExport(string name, EExportType exportType, ExportDataMeta metaData)
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