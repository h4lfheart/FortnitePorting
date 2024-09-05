using FortnitePorting.OnlineServices.Extensions;
using FortnitePorting.OnlineServices.Packet;
using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace FortnitePorting.OnlineServices.Models;

// this has like 3 different ways to serialize/deserialize lol
public class Identification : IDualSerialize
{
    [J("id")] public string Id { get; set; } = string.Empty;
    [J("avatar")] public string AvatarId { get; set; } = string.Empty;
    [J("username")] public string UserName { get; set; } = string.Empty;
    [J("global_name")] public string GlobalName { get; set; } = string.Empty;

    public string DisplayName => !string.IsNullOrWhiteSpace(GlobalName) ? GlobalName : UserName;

    public ERoleType RoleType { get; set; } = ERoleType.User;

    public string AvatarURL => RoleType switch
    {
        ERoleType.System => "https://fortniteporting.halfheart.dev/sockets/system.png",
        ERoleType.SystemExport => "https://fortniteporting.halfheart.dev/sockets/export.png",
        _ => !string.IsNullOrWhiteSpace(AvatarId) ? $"https://cdn.discordapp.com/avatars/{Id}/{AvatarId}.png?size=128" : "https://fortniteporting.halfheart.dev/sockets/default.png"
    };

    public string? Version;

    public static Identification System = new()
    {
        RoleType = ERoleType.System,
        UserName = "system",
        GlobalName = "SYSTEM"
    };
    
    public static Identification Export = new()
    {
        RoleType = ERoleType.SystemExport,
        UserName = "export",
        GlobalName = "EXPORT"
    };

    public bool HasPermission(EPermissions permission)
    {
        return RoleType.GetPermissions().HasFlag(permission);
    }
    
    public void Serialize(BinaryWriter writer)
    {
        writer.Write((int) RoleType);
        writer.Write(Id ?? string.Empty);
        writer.Write(AvatarId ?? string.Empty);
        writer.Write(UserName ?? string.Empty);
        writer.Write(GlobalName ?? string.Empty);
        writer.Write(Version ?? string.Empty);
    }

    public void Deserialize(BinaryReader reader)
    {
        RoleType = (ERoleType) reader.ReadInt32();
        Id = reader.ReadString();
        AvatarId = reader.ReadString();
        UserName = reader.ReadString();
        GlobalName = reader.ReadString();
        Version = reader.ReadString();
    }
}

