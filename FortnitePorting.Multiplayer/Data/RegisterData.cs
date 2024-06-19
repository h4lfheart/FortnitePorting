using FortnitePorting.Multiplayer.Utils;
using GenericReader;

namespace FortnitePorting.Multiplayer.Data;

public class RegisterData : BaseData
{
    public override EDataType DataType => EDataType.Register;
    
    public RegisterData() {}

    public override void Serialize(BinaryWriter Ar)
    {
    }

    public override void Deserialize(GenericBufferReader Ar)
    {
    }
}