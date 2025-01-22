namespace FortnitePorting.OnlineServices.Models;

public abstract class BaseDualSerialize
{
    public abstract void Serialize(BinaryWriter writer);

    public abstract void Deserialize(BinaryReader reader);

    public static T Deserialize<T>(BinaryReader reader) where T : BaseDualSerialize, new()
    {
        var obj = new T();
        obj.Deserialize(reader);
        return obj;
    }
}