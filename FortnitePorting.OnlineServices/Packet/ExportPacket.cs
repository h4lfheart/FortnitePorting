namespace FortnitePorting.OnlineServices.Packet;

public class ExportPacket() : IPacket
{
    public string Path;
    
    public ExportPacket(string path) : this()
    {
        Path = path;
    }

    public EPacketType PacketType => EPacketType.Export;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Path);
    }

    public void Deserialize(BinaryReader reader)
    {
        Path = reader.ReadString();
    }
}