namespace FortnitePorting.OnlineServices.Packet;

public class TitlePacket() : IPacket
{
    public string Title;
    public string Subtitle;
    
    public TitlePacket(string title, string subtitle = "") : this()
    {
        Title = title;
        Subtitle = subtitle;
    }

    public EPacketType PacketType => EPacketType.Title;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Title);
        writer.Write(Subtitle);
    }

    public void Deserialize(BinaryReader reader)
    {
        Title = reader.ReadString();
        Subtitle = reader.ReadString();
    }
}