using System.Text.Json.Serialization;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace FortnitePorting.Models.Supabase.Tables;

[Table("levels")]
public class Levels : BaseModel
{
    [JsonPropertyName("xp")] public long XP { get; set; }
    [JsonPropertyName("level")] public int Level { get; set; }
}
