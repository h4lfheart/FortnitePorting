using System.Globalization;
using FortnitePorting.Multiplayer.Utils;
using GenericReader;

namespace FortnitePorting.Multiplayer.Data;

public class ImageHeaderData : BaseData
{
    public override EDataType DataType => EDataType.ImageHeader;

    public string Name;
    public int Width;
    public int Height;
    public int ChunkCount;
    public DateTime Time;
    public Guid ID;
    public List<Guid> Reactions = [];
    
    public ImageHeaderData() {}
    
    public ImageHeaderData(string name, int width, int height, int chunkCount, List<Guid>? reactions = null, Guid? id = null, DateTime? time = null)
    {
        Name = name;
        Width = width;
        Height = height;
        ChunkCount = chunkCount;
        Time = time ?? DateTime.UtcNow;
        ID = id ?? Guid.NewGuid();
        Reactions = reactions ?? [];
    }

    public override void Serialize(BinaryWriter Ar)
    {
        Ar.WriteFPString(Name);
        Ar.Write(Width);
        Ar.Write(Height);
        Ar.Write(ChunkCount);
        Ar.WriteFPString(Time.ToString(CultureInfo.InvariantCulture));
        Ar.WriteGuid(ID);
        Ar.WriteArray(Reactions, Ar.WriteGuid);
    }

    public override void Deserialize(GenericBufferReader Ar)
    {
        Name = Ar.ReadFPString();
        Width = Ar.Read<int>();
        Height = Ar.Read<int>();
        ChunkCount = Ar.Read<int>();
        
        Time = DateTime.Parse(Ar.ReadFPString(), CultureInfo.InvariantCulture);
        ID = Ar.ReadGuid();

        var count = Ar.Read<int>();
        Reactions = Ar.ReadArray(count, Ar.ReadGuid).ToList();
    }
}