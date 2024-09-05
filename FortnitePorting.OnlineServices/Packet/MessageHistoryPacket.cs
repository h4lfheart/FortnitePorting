using FortnitePorting.OnlineServices.Extensions;
using FortnitePorting.OnlineServices.Models;

namespace FortnitePorting.OnlineServices.Packet;

public class MessageHistoryPacket() : IPacket
{
    public List<MessagePacket> Packets = [];
    public List<MetadataBuilder> Metas = [];
    
    public MessageHistoryPacket(List<MessagePacket> packets, List<MetadataBuilder> metas) : this()
    {
        Packets = packets;
        Metas = metas;
    }

    public EPacketType PacketType => EPacketType.MessageHistory;

    public void Serialize(BinaryWriter writer)
    {
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

    public void Deserialize(BinaryReader reader)
    {
        var packetCount = reader.ReadInt32();
        for (var i = 0; i < packetCount; i++)
        {
            Packets.Add(IDualSerialize.Deserialize<MessagePacket>(reader));
        }

        var metaCount = reader.ReadInt32();
        for (var i = 0; i < metaCount; i++)
        {
            Metas.Add(IDualSerialize.Deserialize<MetadataBuilder>(reader));
        }
    }
}