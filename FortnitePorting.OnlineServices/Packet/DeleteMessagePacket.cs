namespace FortnitePorting.OnlineServices.Packet;

public class DeleteMessagePacket() : IPacket
{
    public EPacketType PacketType => EPacketType.DeleteMessage;

    public void Serialize(BinaryWriter writer)
    {
    }

    public void Deserialize(BinaryReader reader)
    {
    }
}