using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Supabase.User;
using Mapster;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace FortnitePorting.Models.Supabase.Tables;

[Table("permissions")]
[AdaptTo(nameof(UserPermissions)), GenerateMapper]
public class Permissions : BaseModel
{
    [JsonPropertyName("role")] public ESupabaseRole Role { get; set; }
    [JsonPropertyName("uefn_export")] public bool CanExportUEFN { get; set; }
    [JsonPropertyName("is_muted")] public bool IsMuted { get; set; }
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

public class SupabaseRoleStringEnumConverter : JsonConverter<ESupabaseRole>
{
    public override ESupabaseRole Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
            return (ESupabaseRole) reader.GetInt32();

        if (reader.GetString() is { } str && Enum.TryParse(str, true, out ESupabaseRole result))
            return result;

        throw new JsonException($"Unable to convert '{reader.GetString()}' to {nameof(ESupabaseRole)}.");
    }

    public override void Write(Utf8JsonWriter writer, ESupabaseRole value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString().ToLowerInvariant());
    }
}
