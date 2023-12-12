using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using Newtonsoft.Json;

namespace FortnitePorting.Export.Types;

public abstract class ExportDataBase
{
    public string Name;
    public string Type;
    public string PrimitiveType;
    [JsonIgnore] protected readonly ExporterInstance Exporter;

    public ExportDataBase(string name, UObject asset, FStructFallback[] styles, EAssetType type, EExportType primitiveType, EExportTargetType exportType)
    {
        Name = name;
        Type = type.ToString();
        PrimitiveType = primitiveType.ToString();
        Exporter = new ExporterInstance(exportType);
    }

    public void WaitForExports()
    {
        foreach (var task in Exporter.ExportTasks)
        {
            task.Wait();
        }
    }
}