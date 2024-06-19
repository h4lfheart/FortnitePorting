using FortnitePorting.Multiplayer.Utils;
using GenericReader;

namespace FortnitePorting.Multiplayer.Data;

public class UnregisterData : BaseData
{
    public override EDataType DataType => EDataType.Unregister;
    
    public UnregisterData() {}

    public override void Serialize(BinaryWriter Ar)
    {
    }

    public override void Deserialize(GenericBufferReader Ar)
    {
    }
}