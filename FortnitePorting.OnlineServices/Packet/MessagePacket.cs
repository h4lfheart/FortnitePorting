namespace FortnitePorting.OnlineServices.Packet;

public class MessagePacket() : BasePacket
{
    public EMessageVersion DataVersion = EMessageVersion.Latest;
    
    public string Message;
    public byte[] AttachmentData = [];
    public string AttachmentName = string.Empty;
    public bool HasAttachmentData;
    
    public MessagePacket(string message, byte[]? attachmentData = null, string? attachmentName = null) : this()
    {
        Message = message;
        AttachmentData = attachmentData ?? [];
        AttachmentName = attachmentName ?? string.Empty;
        HasAttachmentData = AttachmentData is { Length: > 0 };
    }

    public override EPacketType PacketType => EPacketType.Message;

    public override void Serialize(BinaryWriter writer)
    {
        writer.Write((byte) DataVersion);
        
        writer.Write(HasAttachmentData);
        writer.Write(Message);

        if (HasAttachmentData)
        {
            writer.Write(AttachmentName);
            writer.Write(AttachmentData.Length);
            writer.Write(AttachmentData);
        }
    }

    public override void Deserialize(BinaryReader reader)
    {
        DataVersion = (EMessageVersion) reader.ReadByte();
        
        HasAttachmentData = reader.ReadBoolean();
        Message = reader.ReadString();

        if (HasAttachmentData)
        {
            AttachmentName = reader.ReadString();
            
            var length = reader.ReadInt32();
            AttachmentData = reader.ReadBytes(length);
        }
    }
}

[Flags]
public enum EMessageFlags
{
    Text,
    Image,
    Gif,
    Video,
    File
}

public enum EMessageVersion : byte
{
    BeforeCustomVersionWasAdded,
    
    LatestPlusOne,
    Latest = LatestPlusOne - 1,
}