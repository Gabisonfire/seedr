using System.Diagnostics;
using System.Security.Cryptography;
using System.Data.HashFunction.xxHash;

namespace Seedr
{
    public static class Hashing
    {
        static readonly IxxHash hasher = xxHashFactory.Instance.Create();

        static List<Task> taskPool = new();

        static List<IHashable> bufferPool = new();

        static string Hash(string filePath)
        {
            return Core.config.HashAlgo switch
            {
                "crc32" => GetChecksumCRC32(filePath),
                "md5" => GetChecksumMD5(filePath),
                "md5_invoked" => GetChecksumMD5Invoke(filePath),
                "xxhash64" => GetChecksumxxhash64(filePath),
                _ => "",
            };
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
            Core.logger.Debug($"MD5 Hashing completed. {file}: {timer.Elapsed}");
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

        // Hash in threaded tasks
        public static IEnumerable<IHashable> HashX(IEnumerable<IHashable> filePool, bool WriteToDB = true)
        {   
            // Empty any existing buffer
            bufferPool.Clear();
            var timer = new Stopwatch();
            Core.logger.Info("Hashing torrents, depending on your system, this might take a while.");
            timer.Start();

            // If the division equals zero, ex: 4 torrents but 10 threads allowed, we set stack size to 1 (all torrents in a single stack)
            // which leads to all torrent being hashed on a seperate task since it will iterate over the stack.
            int stackSize = filePool.Count()/Core.config.HashingThreads < 1 ? 1 : filePool.Count()/Core.config.HashingThreads;
            var stacks = filePool.Chunk(stackSize);
            foreach(var pool in stacks)
            {
                foreach(var torrent in pool)
                {
                    torrent.Hashes.Clear();
                    
                    foreach(var file in torrent.FilesList)
                    {
                        Task t = Task.Factory.StartNew(() => {
                            torrent.Hashes.Add(new HashValue
                            (
                                file, Hash(file)
                            ));
                            if(WriteToDB){Database.Write(torrent.ToMySQL());} // Write directly the hash when computed.
                        });
                        taskPool.Add(t);
                    }
                    bufferPool.Add(torrent);

                };
            }
            // Wait for all threads to complete
            Task.WaitAll(taskPool.ToArray());
            foreach(var torrent in bufferPool)
            {
                Core.logger.Debug($"Hashed torrent: {torrent.Name} ({torrent.FilesList.Count()} file(s))");
            }
            timer.Stop();
            Core.logger.Info($"Hashing complete. Hashed {bufferPool.Count} torrents ({taskPool.Count()} file(s)) in {timer.Elapsed} using {Core.config.HashAlgo}.");
            taskPool.Clear();
            return bufferPool;
        }
    }
}