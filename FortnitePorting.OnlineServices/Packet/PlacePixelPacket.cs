using FortnitePorting.OnlineServices.Models;

namespace FortnitePorting.OnlineServices.Packet;

public class PlacePixelPacket() : IPacket
{
    public PlacePixel Pixel;

    public PlacePixelPacket(PlacePixel pixel) : this()
    {
        Pixel = pixel;
    }

    public EPacketType PacketType => EPacketType.PlacePixel;
    
    public void Serialize(BinaryWriter writer)
    {
        Pixel.Serialize(writer);
    }

    public void Deserialize(BinaryReader reader)
    {
        Pixel = IDualSerialize.Deserialize<PlacePixel>(reader);
    }
}