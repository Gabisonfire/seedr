using System.Diagnostics;
using Seedr.Utils;

namespace Seedr
{
    public class HashValue
    {
        public string FilePath {get;} = string.Empty;
        public string FileHash {get;set;} = string.Empty;
        public string RealPath {get;set;} = string.Empty;
        public string Source {get;set;} = string.Empty;
        public long FileSize {get; set;} = 0;

        // public HashValue(string FilePath, string FileHash)
        // {
        //     this.FileHash = FileHash;
        //     RealPath = FilePath;
        //     this.FilePath = Config.Remap(FilePath);
        // }

        public HashValue(string FilePath, string RealPath, string FileHash, string Source, long FileSize)
        {
            this.FilePath = FilePath;
            this.FileHash = FileHash;
            this.RealPath = RealPath;
            this.Source = Source;
            this.FileSize = FileSize;
        }

        public override string ToString()
        {
            return $"{FilePath}: {FileHash}";
        }
    }

    public class HashPair
    {
        public string FromClient {get;} = string.Empty;
        public string FromLibrary {get;} = string.Empty;

        public HashPair(string FromClient, string FromLibrary)
        {
            this.FromClient = FromClient;
            this.FromLibrary = FromLibrary;
        }
    }
}