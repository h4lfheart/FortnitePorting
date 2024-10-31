using FortnitePorting.OnlineServices.Models;
using Serilog;

namespace FortnitePorting.OnlineServices.Packet;

public class CanvasInfoPacket() : IPacket
{
    public ushort X;
    public ushort Y;
    public List<PlacePixel> Pixels = [];

    public CanvasInfoPacket(ushort x, ushort y, List<PlacePixel> pixels) : this()
    {
        X = x;
        Y = y;
        Pixels = pixels;
    }

    public EPacketType PacketType => EPacketType.CanvasInfo;
    
    public void Serialize(BinaryWriter writer)
    {
        writer.Write(X);
        writer.Write(Y);
        
        writer.Write(Pixels.Count);
        foreach (var pixel in Pixels)
        {
            pixel.Serialize(writer);
        }
    }

    public void Deserialize(BinaryReader reader)
    {
        X = reader.ReadUInt16();
        Y = reader.ReadUInt16();

        var pixelCount = reader.ReadInt32();
        for (var i = 0; i < pixelCount; i++)
        {
            Pixels.Add(IDualSerialize.Deserialize<PlacePixel>(reader));
        }
    }
}