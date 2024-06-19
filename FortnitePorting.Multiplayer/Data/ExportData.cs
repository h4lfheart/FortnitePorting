using FortnitePorting.Multiplayer.Utils;
using GenericReader;

namespace FortnitePorting.Multiplayer.Data;

public class ExportData : BaseData
{
    public override EDataType DataType => EDataType.Export;

    public string TargetName;
    public string Path;
    
    public ExportData() {}
    
    public ExportData(string targetName, string path)
    {
        TargetName = targetName;
        Path = path;
    }

    public override void Serialize(BinaryWriter Ar)
    {
        Ar.WriteFPString(TargetName);
        Ar.WriteFPString(Path);
    }

    public override void Deserialize(GenericBufferReader Ar)
    {
        TargetName = Ar.ReadFPString();
        Path = Ar.ReadFPString();
    }
}