using FortnitePorting.OnlineServices.Models;

namespace FortnitePorting.OnlineServices.Packet;

public class OnlineUsersPacket() : BasePacket
{
    public EOnlineUsersVersion DataVersion = EOnlineUsersVersion.Latest;
    
    public List<Identification> Users = [];
    
    public OnlineUsersPacket(List<Identification> users) : this()
    {
        Users = users;
    }

    public override EPacketType PacketType => EPacketType.OnlineUsers;

    public override void Serialize(BinaryWriter writer)
    {
        writer.Write((byte) DataVersion);
        
        writer.Write(Users.Count);
        foreach (var user in Users)
        {
            user.Serialize(writer);
        }
    }

    public override void Deserialize(BinaryReader reader)
    {
        DataVersion = (EOnlineUsersVersion) reader.ReadByte();
        
        var length = reader.ReadInt32();
        for (var i = 0; i < length; i++)
        {
            var user = new Identification();
            user.Deserialize(reader);
            Users.Add(user);
        }
    }
}

public enum EOnlineUsersVersion : byte
{
    BeforeCustomVersionWasAdded,
    
    LatestPlusOne,
    Latest = LatestPlusOne - 1,
}