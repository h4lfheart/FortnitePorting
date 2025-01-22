using FortnitePorting.OnlineServices.Models;
using Serilog;

namespace FortnitePorting.OnlineServices.Packet;

public class CanvasInfoPacket() : BasePacket
{
    public ECanvasInfoVersion DataVersion = ECanvasInfoVersion.Latest;
    
    public ushort X;
    public ushort Y;
    public List<PlacePixel> Pixels = [];

    public CanvasInfoPacket(ushort x, ushort y, List<PlacePixel> pixels) : this()
    {
        X = x;
        Y = y;
        Pixels = pixels;
    }

    public override EPacketType PacketType => EPacketType.CanvasInfo;
    
    public override void Serialize(BinaryWriter writer)
    {
        writer.Write((byte) DataVersion);
        
        writer.Write(X);
        writer.Write(Y);
        
        writer.Write(Pixels.Count);
        foreach (var pixel in Pixels)
        {
            pixel.Serialize(writer);
        }
    }

    public override void Deserialize(BinaryReader reader)
    {
        DataVersion = (ECanvasInfoVersion) reader.ReadByte();
        X = reader.ReadUInt16();
        Y = reader.ReadUInt16();

        var pixelCount = reader.ReadInt32();
        for (var i = 0; i < pixelCount; i++)
        {
            Pixels.Add(BaseDualSerialize.Deserialize<PlacePixel>(reader));
        }
    }
}

public enum ECanvasInfoVersion : byte
{
    BeforeCustomVersionWasAdded,
    
    LatestPlusOne,
    Latest = LatestPlusOne - 1,
}