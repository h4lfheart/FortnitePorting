using System.Globalization;
using FortnitePorting.Multiplayer.Utils;
using GenericReader;

namespace FortnitePorting.Multiplayer.Data;

public class ImageChunkData : BaseData
{
    public override EDataType DataType => EDataType.ImageChunk;

    public int Index;
    public byte[] Data;
    
    public ImageChunkData() {}
    
    public ImageChunkData(int index, byte[] data)
    {
        Index = index;
        Data = data;
    }

    public override void Serialize(BinaryWriter Ar)
    {
        Ar.Write(Index);
        Ar.Write(Data.Length);
        Ar.Write(Data);
    }

    public override void Deserialize(GenericBufferReader Ar)
    {
        Index = Ar.Read<int>();
        
        var length = Ar.Read<int>();
        Data = Ar.ReadArray<byte>(length);
    }
}