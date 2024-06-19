using FortnitePorting.Multiplayer.Utils;
using GenericReader;

namespace FortnitePorting.Multiplayer.Data;

public class PingData : BaseData
{
    public override EDataType DataType => EDataType.Ping;
    
    public PingData() {}

    public override void Serialize(BinaryWriter Ar)
    {
    }

    public override void Deserialize(GenericBufferReader Ar)
    {
    }
}