using System.ComponentModel;

namespace FortnitePorting.OnlineServices.Packet;

public class SetRolePacket() : BasePacket
{
    public ESetRoleVersion DataVersion = ESetRoleVersion.Latest;
    
    public Guid Id;
    public ERoleType Role;

    public SetRolePacket(Guid guid, ERoleType role) : this()
    {
        Id = guid;
        Role = role;
    }
    
    public override EPacketType PacketType => EPacketType.SetRole;
    
    public override void Serialize(BinaryWriter writer)
    {
        writer.Write((byte) DataVersion);
        
        writer.Write(Id.ToString());
        writer.Write((int) Role);
    }

    public override void Deserialize(BinaryReader reader)
    {
        DataVersion = (ESetRoleVersion) reader.ReadByte();
        
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

public enum ESetRoleVersion : byte
{
    BeforeCustomVersionWasAdded,
    
    LatestPlusOne,
    Latest = LatestPlusOne - 1,
}