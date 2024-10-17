namespace FortnitePorting.OnlineServices.Packet;

public class ExportPacket() : IPacket
{
    public string Path;
    public string Message;
    
    public ExportPacket(string path, string message) : this()
    {
        Path = path;
        Message = message;
    }

    public EPacketType PacketType => EPacketType.Export;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Path);
        writer.Write(Message);
    }

    public void Deserialize(BinaryReader reader)
    {
        Path = reader.ReadString();
        Message = reader.ReadString();
    }
}