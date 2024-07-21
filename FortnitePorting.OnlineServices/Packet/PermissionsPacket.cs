using FortnitePorting.OnlineServices.Extensions;

namespace FortnitePorting.OnlineServices.Packet;

public class PermissionsPacket() : IPacket
{
    public EPermissions Permissions;
    public List<string> Commands = [];

    public PermissionsPacket(ERoleType role) : this()
    {
        Permissions = role.GetPermissions();
        Commands = Permissions.GetCommands();
    }
    
    public EPacketType PacketType => EPacketType.Permissions;
    
    public void Serialize(BinaryWriter writer)
    {
        writer.Write((uint) Permissions);
        writer.Write(Commands.Count);
        foreach (var command in Commands)
        {
            writer.Write(command);
        }
    }

    public void Deserialize(BinaryReader reader)
    {
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
    
    SetRole = 1 << 2,
    
    Staff = (1 << 3) - 1,
    
    Owner = 0xFFFFFFFF
}