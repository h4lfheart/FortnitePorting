using System.Collections.Generic;
using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace FortnitePorting.Models.Supabase.Tables;

[Table("exports")]
public class Export : BaseModel
{
    [JsonProperty("object_paths")] 
    public IEnumerable<string> ExportPaths = [];
}