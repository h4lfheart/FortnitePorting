using System;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Models;
using FortnitePorting.Models.Assets;

namespace FortnitePorting.Exporting.Types;

public class BaseExport
{
    public string Name;
    public EExportType Type;
    public EPrimitiveExportType PrimitiveType => Type.GetPrimitiveType();

    protected Context.ExportContext Exporter;

    public BaseExport(string name, UObject asset, BaseStyleData[] styles, EExportType exportType, ExportDataMeta metaData)
    {
        Name = name;
        Type = exportType;

        Exporter = new Context.ExportContext(metaData);
    }
    
    public BaseExport(string name, EExportType exportType, ExportDataMeta metaData)
    {
        Name = name;
        Type = exportType;

        Exporter = new Context.ExportContext(metaData);
    }
    
    public async Task WaitForExports()
    {
        foreach (var task in Exporter.ExportTasks)
        {
            await task.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
}