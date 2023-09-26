using Seedr.Utils;

namespace Seedr 
{

    public interface IHashable
    {
        public string Name {get;}
        public List<HashValue> Hashes {get;set;}
        public string[] FilesList {get; set;}
        public string[] RealFilesList {get; set;}
        public string ToMySQL();
    }

    public class LibraryFile : IHashable
    {
        public string Name {get;} = string.Empty;
        public List<HashValue> Hashes {get;set;} = new List<HashValue>();
        public string[] FilesList {get; set;} = new string[]{};
        public string[] RealFilesList {get; set;} = new string[]{};

        public LibraryFile(string Name, string file)
        {
            this.Name = Name;
            FilesList = new string[]{file};
        }

        public LibraryFile(string file)
        {
            Name = file;
            FilesList = new string[]{file};
        }

        public string ToMySQL()
        {
            string query = "";
            foreach(var hash in Hashes)
            {
                query +=
                @$"
                INSERT OR REPLACE INTO media_files('path', 'hash', 'source') VALUES('{hash.FilePath}', '{hash.FileHash}', 'library');
                " + Environment.NewLine; 
            }
            return query;
        }
    }

    public class Torrent : IHashable
    {
        public string Name {get;} = string.Empty;
        public string TorrentPath {get;} = string.Empty;
        public string RealTorrentPath {get;} = string.Empty;
        public string[] FilesList {get; set;} = new string[]{};
        public string[] RealFilesList {get; set;} = new string[]{};
        public List<HashValue> Hashes {get;set;} = new List<HashValue>();


        public Torrent(string Name, string TorrentPath)
        {
            this.Name = Name;
            RealTorrentPath = TorrentPath;
            this.TorrentPath = Config.Remap(TorrentPath);
            FileAttributes attr = File.GetAttributes(this.TorrentPath);
            if(attr.HasFlag(FileAttributes.Directory)){
                FilesList = Directory.GetFiles(this.TorrentPath, "*", SearchOption.AllDirectories);
            }
            else
            {
                FilesList = new string[]{this.TorrentPath};
            }
            FilterByExtension();
        }

        void FilterByExtension()
        {
            List<string> buffer = FilesList.ToList();
            foreach(var file in FilesList)
            {
                if(!Core.config.ValidExtensions.Contains(Path.GetExtension(file)))
                {
                    buffer.Remove(file);
                }
            }
            FilesList = buffer.ToArray();
        }

        public override string ToString()
        {
            return $"{Name}: {TorrentPath} ({Hashes})";
        }

        public string ToMySQL()
        {
            string query = "";
            foreach(var hash in Hashes)
            {
                query +=
                @$"
                INSERT OR REPLACE INTO media_files('path', 'hash', 'source') VALUES('{hash.FilePath}', '{hash.FileHash}', 'torrent');
                " + Environment.NewLine; 
            }
            return query;
        }
    }


}