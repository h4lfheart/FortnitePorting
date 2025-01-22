using FortnitePorting.OnlineServices.Models;

namespace FortnitePorting.OnlineServices.Packet;

public class CanvasPixelPacket() : BasePacket
{
    public PlacePixel Pixel;

    public CanvasPixelPacket(PlacePixel pixel) : this()
    {
        Pixel = pixel;
    }

    public override EPacketType PacketType => EPacketType.CanvasPixel;
    
    public override void Serialize(BinaryWriter writer)
    {
        Pixel.Serialize(writer);
    }

    public override void Deserialize(BinaryReader reader)
    {
        Pixel = BaseDualSerialize.Deserialize<PlacePixel>(reader);
    }
}