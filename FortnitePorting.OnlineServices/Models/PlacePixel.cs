namespace FortnitePorting.OnlineServices.Models;

public class PlacePixel() : IDualSerialize
{
    public string DatabaseUniqueIdentifier { get; set; }
    public ushort X { get; set; }
    public ushort Y { get; set; }
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }

    public PlacePixel(ushort x, ushort y, byte r, byte g, byte b) : this()
    {
        DatabaseUniqueIdentifier = $"{x},{y}";
        
        X = x;
        Y = y;
        R = r;
        G = g;
        B = b;
    }
        
    public void Serialize(BinaryWriter writer)
    {
        writer.Write(X);
        writer.Write(Y);
        writer.Write(R);
        writer.Write(G);
        writer.Write(B);
    }

    public void Deserialize(BinaryReader reader)
    {
        X = reader.ReadUInt16();
        Y = reader.ReadUInt16();
        R = reader.ReadByte();
        G = reader.ReadByte();
        B = reader.ReadByte();
    }
}