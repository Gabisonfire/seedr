using System.IO;
using System.Security.Cryptography;

namespace Seedr
{
    public static class Hashing
    {
        public static string GetChecksum(string file)
        {
            Core.logger.Debug($"Hashing {file}");
            MD5 hash = MD5.Create();
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 16 * 1024 * 1024);
            byte[] checksum = hash.ComputeHash(stream);
            Core.logger.Debug($"Hashing completed.");
            return BitConverter.ToString(checksum).Replace("-", String.Empty);
        }
    }
}