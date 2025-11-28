using FortnitePorting.Exporting.Types;
using FortnitePorting.ViewModels;
using Newtonsoft.Json;

namespace FortnitePorting.Exporting.Models;

public class ExportData
{
    public ExportDataMeta MetaData;
    public BaseExport[] Exports;
}

public class ExportDataMeta
{
    public string AssetsRoot;
    public BaseExportSettings Settings;

    [JsonIgnore] public EExportLocation ExportLocation;
    [JsonIgnore] public string? CustomPath;
    [JsonIgnore] public EWorldFlags WorldFlags = EWorldFlags.Actors | EWorldFlags.WorldPartitionGrids | EWorldFlags.Landscape | EWorldFlags.InstancedFoliage | EWorldFlags.HLODs;

    public event ExportProgressUpdate UpdateProgress;

    public virtual void OnUpdateProgress(string name, int current, int total)
    {
        UpdateProgress?.Invoke(name, current, total);
    }
}

public delegate void ExportProgressUpdate(string name, int current, int total);

public static class ExportLocationExtensions
{
    extension(EExportLocation exportLocation)
    {
        public bool IsFolder => exportLocation is EExportLocation.AssetsFolder or EExportLocation.CustomFolder;
    }
}