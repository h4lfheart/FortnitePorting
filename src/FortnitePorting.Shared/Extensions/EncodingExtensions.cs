using System.Text;

namespace FortnitePorting.Shared.Extensions;

public static class EncodingExtensions
{
    extension(string text)
    {
        public byte[] StringToBytes()
        {
            return Encoding.UTF8.GetBytes(text);
        }
    }

    extension(byte[] data)
    {
        public string BytesToString()
        {
            return Encoding.UTF8.GetString(data);
        }
    }
}
