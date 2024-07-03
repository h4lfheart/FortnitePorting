namespace FortnitePorting.Multiplayer.Packet;

public class MessagePacket() : IPacket
{
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

    public EPacketType PacketType => EPacketType.Message;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(HasAttachmentData);
        writer.Write(Message);

        if (HasAttachmentData)
        {
            writer.Write(AttachmentName);
            writer.Write(AttachmentData.Length);
            writer.Write(AttachmentData);
        }
    }

    public void Deserialize(BinaryReader reader)
    {
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