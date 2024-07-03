using FortnitePorting.Multiplayer.Extensions;
using FortnitePorting.Multiplayer.Models;
using FortnitePorting.Multiplayer.Packet.Owner;

namespace FortnitePorting.Multiplayer.Packet;

public class PermissionsPacket() : IPacket
{
    public EPermissions Permissions;

    public PermissionsPacket(ERoleType role) : this()
    {
        Permissions = role.GetPermissions();
    }
    
    public EPacketType PacketType => EPacketType.Permissions;
    
    public void Serialize(BinaryWriter writer)
    {
        writer.Write((uint) Permissions);
    }

    public void Deserialize(BinaryReader reader)
    {
        Permissions = (EPermissions) reader.ReadUInt32();
    }
}

[Flags]
public enum EPermissions : uint
{
    None = 0,
    
    Text = 1 << 0,
    SendAttachments = 1 << 1,
    
    SetRole = 1 << 2,
    
    Staff = (1 << 3) - 1,
    
    Owner = 0xFFFFFFFF
}