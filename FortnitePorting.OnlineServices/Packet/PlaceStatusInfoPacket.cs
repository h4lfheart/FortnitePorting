using FortnitePorting.OnlineServices.Models;
using Serilog;

namespace FortnitePorting.OnlineServices.Packet;

public class PlaceStatusInfoPacket() : IPacket
{
    public DateTime NextPixelTime;

    public PlaceStatusInfoPacket(DateTime nextPixelTime) : this()
    {
        NextPixelTime = nextPixelTime;
    }

    public EPacketType PacketType => EPacketType.PlaceStatusInfo;
    
    public void Serialize(BinaryWriter writer)
    {
        writer.Write(NextPixelTime.ToOADate());
    }

    public void Deserialize(BinaryReader reader)
    {
        NextPixelTime = DateTime.FromOADate(reader.ReadDouble());
    }
}