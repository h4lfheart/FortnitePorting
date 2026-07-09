using System.Text.Json;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Supabase.User;
using Mapster;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using JsonException = Newtonsoft.Json.JsonException;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace FortnitePorting.Models.Supabase.Tables;

[Table("permissions")]
[AdaptTo(nameof(UserPermissions)), GenerateMapper]
public class Permissions : BaseModel
{
    [JsonProperty("role")] public ESupabaseRole Role;
    [JsonProperty("uefn_export")] public bool CanExportUEFN;
    [JsonProperty("is_muted")] public bool IsMuted;
}

[JsonConverter(typeof(SupabaseRoleStringEnumConverter))]
public enum ESupabaseRole
{
    [Disabled] Invalid,
    User,
    Verified,
    Support,
    Staff,
    Owner,
    System
}


public class SupabaseRoleStringEnumConverter : StringEnumConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        writer.WriteValue(value?.ToString()?.ToLowerInvariant());
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.Value is string str &&
            Enum.TryParse(objectType, str, true, out var result))
        {
            return result;
        }

        throw new JsonSerializationException($"Unable to convert '{reader.Value}' to {objectType.Name}.");
    }
}