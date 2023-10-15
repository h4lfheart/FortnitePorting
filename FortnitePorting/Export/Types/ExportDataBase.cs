using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports;
using FortnitePorting.Application;
using FortnitePorting.ViewModels;
using Newtonsoft.Json;

namespace FortnitePorting.Export.Types;

public abstract class ExportDataBase
{
    public string Name;
    public EAssetType Type;
    
    [JsonIgnore] public readonly ExportOptionsBase ExportOptions;
    [JsonIgnore] protected readonly ExporterInstance Exporter;

    public ExportDataBase(string name, UObject asset, EAssetType type, EExportType exportType)
    {
        Name = name;
        Type = type;
        ExportOptions = AppSettings.Current.ExportOptions.Get(exportType);
        Exporter = new ExporterInstance(ExportOptions);
    }

    public async Task WaitForExportsAsync()
    {
        await Task.WhenAll(Exporter.Tasks);
    }
}