using System.Diagnostics;
using Seedr.Utils;

namespace Seedr
{
    public class HashValue
    {
        public string FilePath {get;} = string.Empty;
        public string FileHash {get;set;} = string.Empty;
        public string RealPath {get;set;} = string.Empty;

        // public HashValue(string FilePath, string FileHash)
        // {
        //     this.FileHash = FileHash;
        //     RealPath = FilePath;
        //     this.FilePath = Config.Remap(FilePath);
        // }

        public HashValue(string FilePath, string RealPath, string FileHash)
        {
            this.FilePath = FilePath;
            this.FileHash = FileHash;
            this.RealPath = RealPath;
        }

        public override string ToString()
        {
            return $"{FilePath}: {FileHash}";
        }
    }
}