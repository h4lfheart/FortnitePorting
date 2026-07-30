namespace FortnitePorting.Shared.Extensions;

public static class StreamExtensions
{
    extension(Stream stream)
    {
        public byte[] ReadToEnd()
        {
            if (stream.CanSeek)
                stream.Position = 0;
            var bytes = new BinaryReader(stream).ReadBytes((int) stream.Length);
            return bytes;
        }
    }
}
