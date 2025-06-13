using FortnitePorting.Models.Supabase.User;
using Mapster;
using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace FortnitePorting.Models.Supabase.Tables;

[Table("permissions")]
[AdaptTo(nameof(UserPermissions)), GenerateMapper]
public class Permissions : BaseModel
{
    [JsonProperty("role")] public ESupabaseRole Role;
    [JsonProperty("uefn_export")] public bool CanExportUEFN;
}

public enum ESupabaseRole
{
    Muted,
    User,
    Verified,
    Support,
    Staff,
    Owner,
    System
}