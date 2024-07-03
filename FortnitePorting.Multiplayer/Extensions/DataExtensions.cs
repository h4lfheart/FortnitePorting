using System.Text.Json;
using FortnitePorting.Multiplayer.Packet;
using WatsonTcp;

namespace FortnitePorting.Multiplayer.Extensions;

public static class DataExtensions
{
    public static T ReadPacket<T>(this byte[] data) where T : IPacket, new()
    {
        var stream = new MemoryStream(data);
        var reader = new BinaryReader(stream);
        var packet = new T();
        packet.Deserialize(reader);
        return packet;
    }
    
    public static byte[] WritePacket(this IPacket packet)
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);
        packet.Serialize(writer);
        return stream.ToArray();
    }
}