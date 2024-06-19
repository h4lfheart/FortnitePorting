using System.Globalization;
using FortnitePorting.Multiplayer.Utils;
using GenericReader;

namespace FortnitePorting.Multiplayer.Data;

public class MessageData : BaseData
{
    public override EDataType DataType => EDataType.Message;

    public string Text;
    public DateTime Time;
    public Guid ID;
    public List<Guid> Reactions = [];
    
    public MessageData() {}
    
    public MessageData(string text, List<Guid>? reactions = null, Guid? id = null, DateTime? time = null)
    {
        Text = text;
        Time = time ?? DateTime.UtcNow;
        ID = id ?? Guid.NewGuid();
        Reactions = reactions ?? [];
    }

    public override void Serialize(BinaryWriter Ar)
    {
        Ar.WriteFPString(Text);
        Ar.WriteFPString(Time.ToString(CultureInfo.InvariantCulture));
        Ar.WriteGuid(ID);
        Ar.WriteArray(Reactions, Ar.WriteGuid);
    }

    public override void Deserialize(GenericBufferReader Ar)
    {
        Text = Ar.ReadFPString();
        Time = DateTime.Parse(Ar.ReadFPString(), CultureInfo.InvariantCulture);
        ID = Ar.ReadGuid();

        var count = Ar.Read<int>();
        Reactions = Ar.ReadArray(count, Ar.ReadGuid).ToList();
    }
}