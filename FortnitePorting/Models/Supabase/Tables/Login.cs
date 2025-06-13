using System;
using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace FortnitePorting.Models.Supabase.Tables;

[Table("logins")]
public class Login : BaseModel
{
    [JsonProperty("version")] 
    public string Version;
}