namespace FortnitePorting.OnlineServices.Models;

public class PlacePixel() : IDualSerialize
{
    public Guid Id { get; set; }
    public ushort X { get; set; }
    public ushort Y { get; set; }
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
    public string Name { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeletion { get; set; }
        
    public void Serialize(BinaryWriter writer)
    {
        writer.Write(X);
        writer.Write(Y);
        writer.Write(R);
        writer.Write(G);
        writer.Write(B);
        writer.Write(Name ?? string.Empty);
        writer.Write(IsDeletion);
    }

    public void Deserialize(BinaryReader reader)
    {
        X = reader.ReadUInt16();
        Y = reader.ReadUInt16();
        R = reader.ReadByte();
        G = reader.ReadByte();
        B = reader.ReadByte();
        Name = reader.ReadString();
        IsDeletion = reader.ReadBoolean();
    }
}