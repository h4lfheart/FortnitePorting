using FortnitePorting.OnlineServices.Packet;

namespace FortnitePorting.OnlineServices.Extensions;

public static class DataExtensions
{
    public static T ReadPacket<T>(this byte[] data) where T : BasePacket, new()
    {
        var stream = new MemoryStream(data);
        var reader = new BinaryReader(stream);
        var packet = new T();
        packet.Deserialize(reader);
        return packet;
    }
    
    public static byte[] WritePacket(this BasePacket packet)
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);
        packet.Serialize(writer);
        return stream.ToArray();
    }
}