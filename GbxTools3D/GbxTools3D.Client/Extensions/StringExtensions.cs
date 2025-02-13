using System.Security.Cryptography;
using System.Text;

namespace GbxTools3D.Client.Extensions;

public static class StringExtensions
{
    public static string Hash(this string str)
    {
        Span<byte> bytes = stackalloc byte[str.Length * 2];
        if (!Encoding.UTF8.TryGetBytes(str, bytes, out var bytesWritten))
        {
            throw new InvalidOperationException("Failed to encode string");
        }

        Span<byte> hash = stackalloc byte[32];
        
        if (!SHA256.TryHashData(bytes[..bytesWritten], hash, out _))
        {
            throw new InvalidOperationException("Failed to hash string");
        }
        
        return Convert.ToHexString(hash);
    }
}