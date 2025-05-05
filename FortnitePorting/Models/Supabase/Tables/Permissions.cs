using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace FortnitePorting.Models.Supabase.Tables;

[Table("permissions")]
public class Permissions : BaseModel
{
    [JsonProperty("role")] public ESupabaseRole Role;
    [JsonProperty("uefn_export")] public bool CanExportUEFN;
}

public enum ESupabaseRole
{
    User,
    Verified,
    Staff,
    Owner
}