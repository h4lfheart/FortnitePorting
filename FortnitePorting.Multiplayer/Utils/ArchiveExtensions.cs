

using System.Text;
using GenericReader;

namespace FortnitePorting.Multiplayer.Utils;

public static class ArchiveExtensions
{
    public static T ReadFP<T>(this IGenericReader Ar) where T : IDualSerialize, new()
    {
        var obj = new T();
        obj.Deserialize(Ar as GenericBufferReader);
        return obj;
    }
    
    public static void WriteGuid(this BinaryWriter Ar, Guid guid)
    {
        var bytes = guid.ToByteArray();
        Ar.Write(bytes.Length);
        Ar.Write(bytes);
    }
    
    public static Guid ReadGuid(this IGenericReader Ar)
    {
        var length = Ar.Read<int>();
        var bytes = Ar.ReadArray<byte>(length);
        return new Guid(bytes);
    }
    
    public static void WriteFPString(this BinaryWriter Ar, string text)
    {
        var bytes = Encoding.UTF32.GetBytes(text);
        Ar.Write(bytes.Length);
        Ar.Write(bytes);
    }
    
    public static string ReadFPString(this IGenericReader Ar)
    {
        var length = Ar.Read<int>();
        return Ar.ReadString(length, Encoding.UTF32);
    }
    
    public static void WriteArray<T>(this BinaryWriter Ar, List<T> list, Action<T> action)
    {
        Ar.Write(list.Count);
        foreach (var item in list)
        {
            action(item);
        }
    }
    
}