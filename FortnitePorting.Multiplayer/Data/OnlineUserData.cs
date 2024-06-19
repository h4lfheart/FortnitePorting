using FortnitePorting.Multiplayer.Models;
using FortnitePorting.Multiplayer.Utils;
using GenericReader;

namespace FortnitePorting.Multiplayer.Data;

public class OnlineUserData : BaseData
{
    public override EDataType DataType => EDataType.OnlineUsers;

    public List<UserData> OnlineUsers;
    
    public OnlineUserData() {}
    
    public OnlineUserData(List<UserData> onlineUsers)
    {
        OnlineUsers = onlineUsers;
    }

    public override void Serialize(BinaryWriter Ar)
    {
        Ar.WriteArray(OnlineUsers, user => user.Serialize(Ar));
    }

    public override void Deserialize(GenericBufferReader Ar)
    {
        OnlineUsers = Ar.ReadArray(reader => reader.ReadFP<UserData>()).ToList();
    }
}