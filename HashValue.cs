namespace Seedr
{
    public class HashValue
    {
        public string FilePath {get;} = string.Empty;
        public string FileHash {get;set;} = string.Empty;

        public HashValue(string FilePath, string FileHash)
        {
            this.FilePath = FilePath;
            this.FileHash = FileHash;
        }
        public HashValue(string FilePath)
        {
            this.FilePath = FilePath;
        }
    }
}