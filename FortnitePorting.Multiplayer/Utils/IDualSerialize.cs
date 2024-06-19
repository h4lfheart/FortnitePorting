using GenericReader;

namespace FortnitePorting.Multiplayer.Utils;

public interface IDualSerialize
{
    public void Serialize(BinaryWriter Ar);
    public void Deserialize(GenericBufferReader Ar);
}