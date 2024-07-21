using FortnitePorting.OnlineServices.Models;

namespace FortnitePorting.OnlineServices.Packet;

public class OnlineUsersPacket() : IPacket
{
    public List<Identification> Users = [];
    
    public OnlineUsersPacket(List<Identification> users) : this()
    {
        Users = users;
    }

    public EPacketType PacketType => EPacketType.OnlineUsers;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Users.Count);
        foreach (var user in Users)
        {
            user.Serialize(writer);
        }
    }

    public void Deserialize(BinaryReader reader)
    {
        var length = reader.ReadInt32();
        for (var i = 0; i < length; i++)
        {
            var user = new Identification();
            user.Deserialize(reader);
            Users.Add(user);
        }
    }
}