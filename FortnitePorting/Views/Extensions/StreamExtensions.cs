using System.IO;

namespace FortnitePorting.Views.Extensions;

public static class StreamExtensions
{
    public static byte[] ToBytes(this Stream str)
    {
        var bytes = new BinaryReader(str).ReadBytes((int) str.Length);
        return bytes;
    }
}