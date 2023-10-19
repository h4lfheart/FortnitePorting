using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports;
using FortnitePorting.Application;
using FortnitePorting.ViewModels;
using Newtonsoft.Json;

namespace FortnitePorting.Export.Types;

public abstract class ExportDataBase
{
    public string Name;
    public string Type;
    [JsonIgnore] protected readonly ExporterInstance Exporter;

    public ExportDataBase(string name, UObject asset, EAssetType type, EExportType exportType)
    {
        Name = name;
        Type = type.ToString();
        Exporter = new ExporterInstance(exportType);
    }

    public async Task WaitForExportsAsync()
    {
        await Task.WhenAll(Exporter.Tasks);
    }
}