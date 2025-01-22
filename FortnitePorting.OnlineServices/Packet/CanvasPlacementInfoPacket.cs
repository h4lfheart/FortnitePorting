using FortnitePorting.OnlineServices.Models;
using Serilog;

namespace FortnitePorting.OnlineServices.Packet;

public class CanvasPlacementInfoPacket() : BasePacket
{
    public ECanvasPlacementInfoVersion DataVersion = ECanvasPlacementInfoVersion.Latest;
    public DateTime NextPixelTime;

    public CanvasPlacementInfoPacket(DateTime nextPixelTime) : this()
    {
        NextPixelTime = nextPixelTime;
    }

    public override EPacketType PacketType => EPacketType.CanvasPlacementInfo;
    
    public override void Serialize(BinaryWriter writer)
    {
        writer.Write((byte) DataVersion);
        writer.Write(NextPixelTime.ToOADate());
    }

    public override void Deserialize(BinaryReader reader)
    {
        DataVersion = (ECanvasPlacementInfoVersion) reader.ReadByte();
        NextPixelTime = DateTime.FromOADate(reader.ReadDouble());
    }
}

public enum ECanvasPlacementInfoVersion : byte
{
    BeforeCustomVersionWasAdded,
    
    LatestPlusOne,
    Latest = LatestPlusOne - 1,
}