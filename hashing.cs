using System.Diagnostics;
using System.Security.Cryptography;
using System.Data.HashFunction.xxHash;

namespace Seedr
{
    public static class Hashing
    {
        static readonly IxxHash hasher = xxHashFactory.Instance.Create();

        public static Torrent Hash(Torrent torrent)
        {
            switch(Core.config.HashAlgo){
                case "crc32":
                torrent.Hash = GetChecksumCRC32(torrent.Path);
                return torrent;
                case "md5":
                torrent.Hash = GetChecksumMD5(torrent.Path);
                return torrent;
                case "md5_invoked":
                torrent.Hash = GetChecksumMD5Invoke(torrent.Path);
                return torrent;
                case "xxhash64":
                torrent.Hash = GetChecksumxxhash64(torrent.Path);
                return torrent;
            }
            return torrent;
        }
        static string GetChecksumMD5(string file)
        {
            var timer = new Stopwatch();
            timer.Start();
            Core.logger.Debug($"MD5 Hashing {file}");
            MD5 hash = MD5.Create();
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 16 * 1024 * 1024);
            byte[] checksum = hash.ComputeHash(stream);
            timer.Stop();
            Core.logger.Debug($"MD5 Hashing completed. {timer.Elapsed}");
            return BitConverter.ToString(checksum).Replace("-", String.Empty);
        }

        static string GetChecksumMD5Invoke(string file)
        {
            var timer = new Stopwatch();
            timer.Start();
            Core.logger.Debug($"Invoke MD5 Hashing {file}");
            var p = new Process ();
            p.StartInfo.FileName = "md5sum";
            p.StartInfo.Arguments = $"\"{file}\"";            
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            p.WaitForExit();
            timer.Stop();
            Core.logger.Debug($"Invoke MD5 Hashing completed. {timer.Elapsed}");      
            string output = p.StandardOutput.ReadToEnd();
            Core.logger.Debug($"Raw output for md5 invoke: {output}");
            return output.Split(' ')[0].ToUpper();
        }

        static string GetChecksumxxhash64(string file)
        {
            var timer = new Stopwatch();
            timer.Start();
            Core.logger.Debug($"xxh64 Hashing {file}");
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 16 * 1024 * 1024);
            var checksum = hasher.ComputeHash(stream);
            timer.Stop();
            Core.logger.Debug($"xxh64 Hashing completed. {timer.Elapsed}");
            return string.Format("0x{0:x}", checksum);
        }

        static string GetChecksumCRC32(string file)
        {
            var timer = new Stopwatch();
            timer.Start();
            Core.logger.Debug($"CRC32 Hashing {file}");
            var crc32 = new System.IO.Hashing.Crc32();
            using var stream = new FileStream(file, FileMode.Open);
            crc32.Append(stream);
            var checksum = crc32.GetCurrentHash();
            timer.Stop();
            Core.logger.Debug($"CRC32 Hashing completed. {timer.Elapsed}");
            return BitConverter.ToString(checksum).Replace("-", String.Empty);
        }
    }
}