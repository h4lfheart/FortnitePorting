using FortnitePorting.Multiplayer.Utils;
using GenericReader;

namespace FortnitePorting.Multiplayer.Data;

public class ReactionData : BaseData
{
    public override EDataType DataType => EDataType.Reaction;

    public Guid MessageID;
    public Guid UserID;
    public bool Increment;
    
    public ReactionData() {}
    
    public ReactionData(Guid messageId, Guid userId, bool increment)
    {
        MessageID = messageId;
        UserID = userId;
        Increment = increment;
    }

    public override void Serialize(BinaryWriter Ar)
    {
        Ar.WriteGuid(MessageID);
        Ar.WriteGuid(UserID);
        Ar.Write(Increment);
    }

    public override void Deserialize(GenericBufferReader Ar)
    {
        MessageID = Ar.ReadGuid();
        UserID = Ar.ReadGuid();
        Increment = Ar.Read<bool>();
    }
}