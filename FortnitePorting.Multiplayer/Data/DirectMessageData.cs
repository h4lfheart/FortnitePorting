using System.Globalization;
using FortnitePorting.Multiplayer.Utils;
using GenericReader;

namespace FortnitePorting.Multiplayer.Data;

public class DirectMessageData : BaseData
{
    public override EDataType DataType => EDataType.DirectMessage;

    public string TargetName;
    public string Text;
    
    public DirectMessageData() {}
    
    public DirectMessageData(string targetName, string text)
    {
        TargetName = targetName;
        Text = text;
    }

    public override void Serialize(BinaryWriter Ar)
    {
        Ar.WriteFPString(TargetName);
        Ar.WriteFPString(Text);
    }

    public override void Deserialize(GenericBufferReader Ar)
    {
        TargetName = Ar.ReadFPString();
        Text = Ar.ReadFPString();
    }
}