using FortnitePorting.OnlineServices.Extensions;

namespace FortnitePorting.OnlineServices.Packet;

public class PermissionsPacket() : BasePacket
{
    public EPermissionsVersion DataVersion = EPermissionsVersion.Latest;
    
    public EPermissions Permissions;
    public List<string> Commands = [];

    public PermissionsPacket(ERoleType role) : this()
    {
        Permissions = role.GetPermissions();
        Commands = Permissions.GetCommands();
    }
    
    public override EPacketType PacketType => EPacketType.Permissions;
    
    public override void Serialize(BinaryWriter writer)
    {
        writer.Write((byte) DataVersion);
        
        writer.Write((uint) Permissions);
        writer.Write(Commands.Count);
        foreach (var command in Commands)
        {
            writer.Write(command);
        }
    }

    public override void Deserialize(BinaryReader reader)
    {
        DataVersion = (EPermissionsVersion) reader.ReadByte();
        
        Permissions = (EPermissions) reader.ReadUInt32();

        var commandCount = reader.ReadInt32();
        for (var i = 0; i < commandCount; i++)
        {
            Commands.Add(reader.ReadString());
        }
    }
}

[Flags]
public enum EPermissions : uint
{
    None = 0,
    
    Text = 1 << 0,
    SendAttachments = 1 << 1,
    LoadPluginFiles = 1 << 2,
    
    SetRole = 1 << 3,
    
    Staff = (1 << 4) - 1,
    
    Owner = 0xFFFFFFFF
}

public enum EPermissionsVersion : byte
{
    BeforeCustomVersionWasAdded,
    
    LatestPlusOne,
    Latest = LatestPlusOne - 1,
}