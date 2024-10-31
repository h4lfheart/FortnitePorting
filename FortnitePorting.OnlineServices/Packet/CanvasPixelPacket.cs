using FortnitePorting.OnlineServices.Models;

namespace FortnitePorting.OnlineServices.Packet;

public class CanvasPixelPacket() : IPacket
{
    public PlacePixel Pixel;

    public CanvasPixelPacket(PlacePixel pixel) : this()
    {
        Pixel = pixel;
    }

    public EPacketType PacketType => EPacketType.CanvasPixel;
    
    public void Serialize(BinaryWriter writer)
    {
        Pixel.Serialize(writer);
    }

    public void Deserialize(BinaryReader reader)
    {
        Pixel = IDualSerialize.Deserialize<PlacePixel>(reader);
    }
}