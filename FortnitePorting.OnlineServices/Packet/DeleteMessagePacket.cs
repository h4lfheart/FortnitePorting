namespace FortnitePorting.OnlineServices.Packet;

public class DeleteMessagePacket() : BasePacket
{
    public EDeleteMessageVersion DataVersion = EDeleteMessageVersion.Latest;
    
    public override EPacketType PacketType => EPacketType.DeleteMessage;

    public override void Serialize(BinaryWriter writer)
    {
        writer.Write((byte) DataVersion);
    }

    public override void Deserialize(BinaryReader reader)
    {
        DataVersion = (EDeleteMessageVersion) reader.ReadByte();
    }
}

public enum EDeleteMessageVersion : byte
{
    BeforeCustomVersionWasAdded,
    
    LatestPlusOne,
    Latest = LatestPlusOne - 1,
}