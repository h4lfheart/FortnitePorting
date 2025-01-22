namespace FortnitePorting.OnlineServices.Packet;

public class ReactionPacket() : BasePacket
{
    public EReactionVersion DataVersion = EReactionVersion.Latest;
    
    public Guid MessageId;
    public bool Increment;

    public ReactionPacket(Guid messageId, bool increment) : this()
    {
        MessageId = messageId;
        Increment = increment;
    }
    
    public override EPacketType PacketType => EPacketType.Reaction;
    
    
    public override void Serialize(BinaryWriter writer)
    {
        writer.Write((byte) DataVersion);
        
        writer.Write(MessageId.ToString());
        writer.Write(Increment);
    }

    public override void Deserialize(BinaryReader reader)
    {
        DataVersion = (EReactionVersion) reader.ReadByte();
        
        MessageId = Guid.Parse(reader.ReadString());
        Increment = reader.ReadBoolean();
    }
}

public enum EReactionVersion : byte
{
    BeforeCustomVersionWasAdded,
    
    LatestPlusOne,
    Latest = LatestPlusOne - 1,
}