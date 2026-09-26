using System.Security.Cryptography;
using System.Text;

namespace FortnitePorting.Shared.Extensions;

public static class HashExtensions
{
    extension(Stream stream)
    {
        public string GetHash()
        {
            return BitConverter.ToString(SHA256.HashData(stream.ReadToEnd())).Replace("-", string.Empty);
        }
    }

    extension(string path)
    {
        public string GetHash()
        {
            return BitConverter.ToString(SHA256.HashData(File.ReadAllBytes(path))).Replace("-", string.Empty);
        }
    }

    extension(FileInfo fileInfo)
    {
        public string GetHash()
        {
            return fileInfo.FullName.GetHash();
        }

        public string GetFileHashMD5()
        {
            using var stream = fileInfo.OpenRead();
            using var sha = MD5.Create();
            var hash = sha.ComputeHash(stream);
            return Convert.ToHexString(hash).ToLower();
        }
    }
}
