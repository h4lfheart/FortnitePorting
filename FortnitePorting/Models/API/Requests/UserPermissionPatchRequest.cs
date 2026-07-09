using FortnitePorting.Models.Supabase.Tables;
using Newtonsoft.Json;

namespace FortnitePorting.Models.API.Requests;

public class UserPermissionPatchRequest
{
    [JsonProperty("role")] public ESupabaseRole? Role;
    [JsonProperty("canExportUEFN")] public bool? CanExportUEFN;
    [JsonProperty("isMuted")] public bool? IsMuted;
}