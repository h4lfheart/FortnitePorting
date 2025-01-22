namespace FortnitePorting.OnlineServices.Packet;

public class ExportPacket() : BasePacket
{
    public EExportVersion DataVersion = EExportVersion.Latest;
    
    public string Path;
    public string Message;
    
    public ExportPacket(string path, string message) : this()
    {
        Path = path;
        Message = message;
    }

    public override EPacketType PacketType => EPacketType.Export;

    public override void Serialize(BinaryWriter writer)
    {
        writer.Write((byte) DataVersion);
        writer.Write(Path);
        writer.Write(Message);
    }

    public override void Deserialize(BinaryReader reader)
    {
        DataVersion = (EExportVersion) reader.ReadByte();
        Path = reader.ReadString();
        Message = reader.ReadString();
    }
}

public enum EExportVersion : byte
{
    BeforeCustomVersionWasAdded,
    
    LatestPlusOne,
    Latest = LatestPlusOne - 1,
}