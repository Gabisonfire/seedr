using Seedr.Utils;

namespace Seedr 
{

    public interface IHashable
    {
        public string Name {get;}
        public List<HashValue> Hashes {get;set;}
        public string[] FilesList {get; set;}
        public string ToMySQL();
    }

    public class LibraryFile : IHashable
    {
        public string Name {get;} = string.Empty;
        public List<HashValue> Hashes {get;set;} = new List<HashValue>();
        public string[] FilesList {get; set;} = new string[]{};

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
                INSERT OR REPLACE INTO media_files('mapped_path', 'real_path', 'hash', 'source') VALUES('{hash.FilePath}', '{hash.RealPath}', '{hash.FileHash}', 'library');
                " + Environment.NewLine; 
            }
            return query;
        }
    }

    public class Torrent : IHashable
    {
        public string Name {get;} = string.Empty;
        public string TorrentPath {get;} = string.Empty;
        public string MappedTorrentPath {get; set;} = string.Empty;
        public string[] FilesList {get; set;} = new string[]{};
        public List<HashValue> Hashes {get;set;} = new List<HashValue>();


        public Torrent(string Name, string TorrentPath)
        {
            this.Name = Name;
            this.TorrentPath = TorrentPath;
            MappedTorrentPath = Config.Remap(TorrentPath);
            FileAttributes attr = File.GetAttributes(MappedTorrentPath);
            if(attr.HasFlag(FileAttributes.Directory)){
                // Here we populate the file list from the mapped path because we need the directory info right away
                // but later in HashX, it will get remapped to we want to store the "real" path back. True here reverts the mapping.
                FilesList = Directory.GetFiles(MappedTorrentPath, "*", SearchOption.AllDirectories).Select(x => Config.Remap(x, true)).ToArray();
            }
            else
            {
                FilesList = new string[]{TorrentPath};
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
                INSERT OR REPLACE INTO media_files('mapped_path', 'real_path', 'hash', 'source') VALUES('{hash.FilePath}', '{hash.RealPath}', '{hash.FileHash}', 'torrent');
                " + Environment.NewLine; 
            }
            return query;
        }
    }


}