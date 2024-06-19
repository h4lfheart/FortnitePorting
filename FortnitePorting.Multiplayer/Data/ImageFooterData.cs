using System.Globalization;
using FortnitePorting.Multiplayer.Utils;
using GenericReader;

namespace FortnitePorting.Multiplayer.Data;

public class ImageFooterData : BaseData
{
    public override EDataType DataType => EDataType.ImageFooter;
    
    public ImageFooterData() {}

    public override void Serialize(BinaryWriter Ar)
    {
    }

    public override void Deserialize(GenericBufferReader Ar)
    {
    }
}