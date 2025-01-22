namespace FortnitePorting.OnlineServices.Packet;

public class TitlePacket() : BasePacket
{
    public ETitleVersion DataVersion = ETitleVersion.Latest;
    
    public string Title;
    public string Subtitle;
    
    public TitlePacket(string title, string subtitle = "") : this()
    {
        Title = title;
        Subtitle = subtitle;
    }

    public override EPacketType PacketType => EPacketType.Title;

    public override void Serialize(BinaryWriter writer)
    {
        writer.Write((byte) DataVersion);
        
        writer.Write(Title);
        writer.Write(Subtitle);
    }

    public override void Deserialize(BinaryReader reader)
    {
        DataVersion = (ETitleVersion) reader.ReadByte();
        
        Title = reader.ReadString();
        Subtitle = reader.ReadString();
    }
}

public enum ETitleVersion : byte
{
    BeforeCustomVersionWasAdded,
    
    LatestPlusOne,
    Latest = LatestPlusOne - 1,
}