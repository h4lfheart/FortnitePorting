using FortnitePorting.Multiplayer.Models;
using FortnitePorting.Multiplayer.Packet;
using FortnitePorting.Multiplayer.Packet.Owner;

namespace FortnitePorting.Multiplayer.Extensions;

public static class MiscExtensions
{
    public static bool IsEmpty(this Guid guid)
    {
        return guid.Equals(Guid.Empty);
    }
    
    public static Guid ToFpGuid(this string id)
    {
        var longValue = long.Parse(id);
        var longBytes = BitConverter.GetBytes(longValue);
        var allBytes = (byte[]) [..longBytes, 0, 0, 0, 0, 0, 0, 0, 0];
        return new Guid(allBytes);
    }
    
    public static string ToDiscordId(this Guid guid)
    {
        var allBytes = guid.ToByteArray();
        var longBytes = allBytes[..8];
        var longValue = BitConverter.ToInt64(longBytes);
        return longValue.ToString();
    }
    
    public static EPermissions GetPermissions(this ERoleType role) => role switch
    {
        ERoleType.Muted => EPermissions.None,
        ERoleType.User => EPermissions.Text,
        ERoleType.ImagePermissionUser => EPermissions.Text | EPermissions.SendAttachments,
        ERoleType.Staff => EPermissions.Staff,
        ERoleType.Owner => EPermissions.Owner,
        _ => EPermissions.Text,
    };
}