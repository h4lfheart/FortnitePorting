using System;
using FortnitePorting.Export.Types;
using FortnitePorting.Shared;
using FortnitePorting.ViewModels;
using Newtonsoft.Json;

namespace FortnitePorting.Export.Models;

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
    [JsonIgnore] public EWorldFlags WorldFlags = EWorldFlags.Actors | EWorldFlags.WorldPartitionGrids | EWorldFlags.Landscape | EWorldFlags.InstancedFoliage;

    public event ExportProgressUpdate UpdateProgress;

    public virtual void OnUpdateProgress(string name, int current, int total)
    {
        UpdateProgress?.Invoke(name, current, total);
    }
}

public delegate void ExportProgressUpdate(string name, int current, int total);

public static class ExportLocationExtensions
{
    public static bool IsFolder(this EExportLocation exportLocation)
    {
        return exportLocation is EExportLocation.AssetsFolder or EExportLocation.CustomFolder;
    }
}