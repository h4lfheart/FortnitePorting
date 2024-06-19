using FortnitePorting.Multiplayer.Utils;
using GenericReader;

namespace FortnitePorting.Multiplayer.Data;

public class UserData : IDualSerialize
{
    public Guid ID = Guid.Empty;
    public string Name = string.Empty;
    public string AvatarURL = string.Empty;
    
    public void Serialize(BinaryWriter Ar)
    {
        Ar.WriteGuid(ID);
        Ar.WriteFPString(Name);
        Ar.WriteFPString(AvatarURL);
    }

    public void Deserialize(GenericBufferReader Ar)
    {
        ID = Ar.ReadGuid();
        Name = Ar.ReadFPString();
        AvatarURL = Ar.ReadFPString();
    }
}