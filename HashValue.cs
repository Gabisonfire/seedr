using Seedr.Utils;

namespace Seedr
{
    public class HashValue
    {
        public string FilePath {get;} = string.Empty;
        public string FileHash {get;set;} = string.Empty;

        public string RealPath {get;} = string.Empty;

        public HashValue(string FilePath, string FileHash)
        {
            this.FilePath = FilePath;
            this.FileHash = FileHash;
            RealPath = FilePath;
            this.FilePath = Config.Remap(FilePath);
            Console.WriteLine(this.FilePath);
        }

        public override string ToString()
        {
            return $"{FilePath}: {FileHash}";
        }
    }
}