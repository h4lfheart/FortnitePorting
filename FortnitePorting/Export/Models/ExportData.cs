using System;
using FortnitePorting.Export.Types;
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

    public event ExportProgressUpdate UpdateProgress;

    public virtual void OnUpdateProgress(string name, int current, int total)
    {
        UpdateProgress?.Invoke(name, current, total);
    }
}

public delegate void ExportProgressUpdate(string name, int current, int total);