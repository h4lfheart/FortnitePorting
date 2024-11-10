using System.ComponentModel;

namespace FortnitePorting.OnlineServices.Packet;

public class SetRolePacket() : IPacket
{
    public Guid Id;
    public ERoleType Role;

    public SetRolePacket(Guid guid, ERoleType role) : this()
    {
        Id = guid;
        Role = role;
    }
    
    public EPacketType PacketType => EPacketType.SetRole;
    
    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Id.ToString());
        writer.Write((int) Role);
    }

    public void Deserialize(BinaryReader reader)
    {
        Id = Guid.Parse(reader.ReadString());
        Role = (ERoleType)reader.ReadInt32();
    }
}

public enum ERoleType
{
    [Description("User")]
    User,
    
    [Description("Trusted")]
    Trusted,
    
    [Description("Verified")]
    Verified,
    
    [Description("Muted")]
    Muted,
    
    [Description("Staff")]
    Staff,
    
    [Description("Owner")]
    Owner,
    
    System,
    SystemExport
}