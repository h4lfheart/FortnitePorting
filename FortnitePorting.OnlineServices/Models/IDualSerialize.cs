namespace FortnitePorting.OnlineServices.Models;

public interface IDualSerialize
{
    public void Serialize(BinaryWriter writer);

    public void Deserialize(BinaryReader reader);
}