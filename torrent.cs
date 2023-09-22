namespace Seedr 
{

    public class Torrent
    {
        public string Name {get;} = string.Empty;
        public string Path {get;} = string.Empty;
        public string Hash {get;set;} = string.Empty;

        public Torrent(string Name, string Path)
        {
            this.Name = Name;
            this.Path = Path;
        }

        public override string ToString()
        {
            return $"{Name}: {Path} ({Hash})";
        }

        public string ToMySQL()
        {
            return
            @$"
            INSERT OR REPLACE INTO download_files('path', 'hash') VALUES('{Path}', '{Hash}');
            ";
        }
    }


}