using FortnitePorting.OnlineServices.Extensions;
using FortnitePorting.OnlineServices.Models;

namespace FortnitePorting.OnlineServices.Packet;

public class MessageHistoryPacket() : BasePacket
{
    public EMessageHistoryVersion DataVersion = EMessageHistoryVersion.Latest;
    
    public List<MessagePacket> Packets = [];
    public List<MetadataBuilder> Metas = [];
    
    public MessageHistoryPacket(List<MessagePacket> packets, List<MetadataBuilder> metas) : this()
    {
        Packets = packets;
        Metas = metas;
    }

    public override EPacketType PacketType => EPacketType.MessageHistory;

    public override void Serialize(BinaryWriter writer)
    {
        writer.Write((byte) DataVersion);
        
        writer.Write(Packets.Count);
        foreach (var packet in Packets)
        {
            writer.Write(packet.WritePacket());
        }
        
        writer.Write(Metas.Count);
        foreach (var meta in Metas)
        {
            meta.Serialize(writer);
        }
    }

    public override void Deserialize(BinaryReader reader)
    {
        DataVersion = (EMessageHistoryVersion) reader.ReadByte();
        
        var packetCount = reader.ReadInt32();
        for (var i = 0; i < packetCount; i++)
        {
            Packets.Add(BaseDualSerialize.Deserialize<MessagePacket>(reader));
        }

        var metaCount = reader.ReadInt32();
        for (var i = 0; i < metaCount; i++)
        {
            Metas.Add(BaseDualSerialize.Deserialize<MetadataBuilder>(reader));
        }
    }
}

public enum EMessageHistoryVersion : byte
{
    BeforeCustomVersionWasAdded,
    
    LatestPlusOne,
    Latest = LatestPlusOne - 1,
}