using System.Text;
using FortnitePorting.Multiplayer.Utils;
using GenericReader;

namespace FortnitePorting.Multiplayer.Data;

public class DataHeader : IDualSerialize
{
    public EDataType DataType;
    
    private const string MAGIC = "FPSOCKET3";
    
    public DataHeader() {}

    public DataHeader(EDataType dataType)
    {
        DataType = dataType;
    }

    public virtual void Serialize(BinaryWriter Ar)
    {
        Ar.Write(Encoding.UTF8.GetBytes(MAGIC));
        Ar.Write((byte) DataType);
    }

    public virtual void Deserialize(GenericBufferReader Ar)
    {
        var magic = Ar.ReadString(MAGIC.Length, Encoding.UTF8);
        if (magic != MAGIC)
        {
            throw new Exception($"Invalid socket header magic. Got {magic}, Expected {MAGIC}");
        }

        DataType = Ar.Read<EDataType>();
    }
}

public enum EDataType : byte
{
    None,
    Register,
    Unregister,
    Ping,
    Message,
    Reaction,
    OnlineUsers,
    Export,
    DirectMessage
}
