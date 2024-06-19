using FortnitePorting.Multiplayer.Utils;
using GenericReader;

namespace FortnitePorting.Multiplayer.Data;

public class BaseData : IDualSerialize
{
    public virtual EDataType DataType => EDataType.None;
    
    public BaseData() {}
    
    public virtual void Serialize(BinaryWriter Ar)
    {
    }

    public virtual void Deserialize(GenericBufferReader Ar)
    {
    }
}