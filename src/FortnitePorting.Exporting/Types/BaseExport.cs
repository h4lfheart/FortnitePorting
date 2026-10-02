using System;
using System.Threading.Tasks;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Models;
using Newtonsoft.Json;

namespace FortnitePorting.Exporting.Types;

public class BaseExport
{
    public string Name;
    public EExportType Type;
    public EPrimitiveExportType PrimitiveType => Type.PrimitiveType;

    [JsonIgnore] public List<string> FolderPaths = [];

    protected Context.ExportContext Context;
    
    public BaseExport(string name, EExportType exportType, ExportDataMeta metaData)
    {
        Name = name;
        Type = exportType;

        Context = new Context.ExportContext(metaData);
    }
    
    public async Task WaitForExports()
    {
        foreach (var task in Context.ExportTasks)
        {
            await task.WaitAsync(TimeSpan.FromSeconds(60));
        }
    }
}