using FortnitePorting.OnlineServices.Models;
using Serilog;

namespace FortnitePorting.OnlineServices.Packet;

public class CanvasPlacementInfoPacket() : IPacket
{
    public DateTime NextPixelTime;

    public CanvasPlacementInfoPacket(DateTime nextPixelTime) : this()
    {
        NextPixelTime = nextPixelTime;
    }

    public EPacketType PacketType => EPacketType.CanvasPlacementInfo;
    
    public void Serialize(BinaryWriter writer)
    {
        writer.Write(NextPixelTime.ToOADate());
    }

    public void Deserialize(BinaryReader reader)
    {
        NextPixelTime = DateTime.FromOADate(reader.ReadDouble());
    }
}