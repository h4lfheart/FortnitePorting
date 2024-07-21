namespace FortnitePorting.OnlineServices.Packet;

public class ReactionPacket() : IPacket
{
    public Guid MessageId;
    public bool Increment;

    public ReactionPacket(Guid messageId, bool increment) : this()
    {
        MessageId = messageId;
        Increment = increment;
    }
    
    public EPacketType PacketType => EPacketType.Reaction;
    
    
    public void Serialize(BinaryWriter writer)
    {
        writer.Write(MessageId.ToString());
        writer.Write(Increment);
    }

    public void Deserialize(BinaryReader reader)
    {
        MessageId = Guid.Parse(reader.ReadString());
        Increment = reader.ReadBoolean();
    }
}