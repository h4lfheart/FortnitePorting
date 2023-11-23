using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using Newtonsoft.Json;

namespace FortnitePorting.Export.Types;

public abstract class ExportDataBase
{
    public string Name;
    public string Type;
    [JsonIgnore] protected readonly ExporterInstance Exporter;

    public ExportDataBase(string name, UObject asset, FStructFallback[] styles, EAssetType type, EExportType exportType)
    {
        Name = name;
        Type = type.ToString();
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