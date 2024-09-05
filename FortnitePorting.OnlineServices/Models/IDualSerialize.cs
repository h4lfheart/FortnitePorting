namespace FortnitePorting.OnlineServices.Models;

public interface IDualSerialize
{
    public void Serialize(BinaryWriter writer);

    public void Deserialize(BinaryReader reader);

    public static T Deserialize<T>(BinaryReader reader) where T : IDualSerialize, new()
    {
        var obj = new T();
        obj.Deserialize(reader);
        return obj;
    }
}