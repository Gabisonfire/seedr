using Microsoft.Data.Sqlite;
using NLog;
using NLog.Fluent;
using Seedr.Utils;

namespace Seedr 
{

    public enum SQLInsertMode { replace, ignore };
    public interface IHashable
    {
        public string Name {get;}
        public List<HashValue> Hashes {get;set;}
        public string[] FilesList {get; set;}
        public string ToMySQL(SQLInsertMode sqlInsertMode = SQLInsertMode.replace, bool newFile = false);
        public SqliteCommand[] ToMySQLCommands(SQLInsertMode sqlInsertMode = SQLInsertMode.replace, bool newFile = false);
        public string FileType {get;}
    }

    public class LibraryFile : IHashable
    {
        public string Name {get;} = string.Empty;
        public List<HashValue> Hashes {get;set;} = new List<HashValue>();
        public string[] FilesList {get; set;} = Array.Empty<string>();
        public string FileType {get;} =  FileSource.Library;

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

        public string ToMySQL(SQLInsertMode sqlInsertMode = SQLInsertMode.replace, bool newFile = false)
        {
            string operation = sqlInsertMode == SQLInsertMode.ignore ? "IGNORE": "REPLACE";
            string query = "";
            if(newFile)
            {
                foreach(var file in FilesList)
                {
                    query +=
                    @$"
                    INSERT OR {operation} INTO media_files('real_path', 'source') VALUES('{file}', 'library');
                    " + Environment.NewLine; 
                }
            }
            else 
            {
                foreach(var hash in Hashes)
                {
                    query +=
                    @$"
                    INSERT OR {operation} INTO media_files('mapped_path', 'real_path', 'hash', 'source') VALUES('{hash.FilePath}', '{hash.RealPath}', '{hash.FileHash}', 'library');
                    " + Environment.NewLine; 
                }
            }
            return query;
        }

        public SqliteCommand[] ToMySQLCommands(SQLInsertMode sqlInsertMode = SQLInsertMode.replace, bool newFile = false)
        {
            string operation = sqlInsertMode == SQLInsertMode.ignore ? "IGNORE": "REPLACE";
            List<SqliteCommand> commandsBuffer = new();
            if(newFile)
            {
                foreach(var file in FilesList)
                {
                    var cmd = new SqliteCommand
                    {
                        CommandText = $"INSERT OR {operation} INTO media_files('real_path', 'source') VALUES($real_path, 'library')"
                    };
                    cmd.Parameters.AddWithValue("$real_path", file);
                    commandsBuffer.Add(cmd);
                }
            }
            else 
            {
                foreach(var hash in Hashes)
                {
                    var cmd = new SqliteCommand
                    {
                        CommandText = $"INSERT OR {operation} INTO media_files('mapped_path', 'real_path', 'hash', 'source') VALUES($file_path, $real_path, $hash, 'library')"
                    };
                    cmd.Parameters.AddWithValue("$file_path", hash.FileHash);
                    cmd.Parameters.AddWithValue("$real_path", hash.RealPath);
                    cmd.Parameters.AddWithValue("$hash", hash.FileHash);
                    commandsBuffer.Add(cmd);
                }
            }
            return commandsBuffer.ToArray();
        }

    }

    public class Torrent : IHashable
    {
        public string Name {get;} = string.Empty;
        public string TorrentPath {get;} = string.Empty;
        public string MappedTorrentPath {get; set;} = string.Empty;
        public string[] FilesList {get; set;} = Array.Empty<string>();
        public List<HashValue> Hashes {get;set;} = new List<HashValue>();
        public string FileType {get; } =  FileSource.Torrent;


        public Torrent(string Name, string TorrentPath)
        {
            this.Name = Name;
            this.TorrentPath = TorrentPath;
            MappedTorrentPath = Config.Remap(TorrentPath);
            try 
            {
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
            catch(FileNotFoundException)
            {
                Core.logger.Warn($"The file '{TorrentPath}' is in your client, but can't be found.");
            }
            catch(DirectoryNotFoundException)
            {
                Core.logger.Warn($"The directory '{TorrentPath}' is in your client, but can't be found.");
            }

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

        public string ToMySQL(SQLInsertMode sqlInsertMode = SQLInsertMode.replace, bool newFile = false)
        {
            string operation = sqlInsertMode == SQLInsertMode.ignore ? "IGNORE": "REPLACE";
            string query = "";
            if(newFile)
            {
                foreach(var file in FilesList)
                {
                    query +=
                    @$"
                    INSERT OR {operation} INTO media_files('real_path', 'source') VALUES('{file}', 'library');
                    " + Environment.NewLine; 
                }
            }
            else
            {
                foreach(var hash in Hashes)
                {
                    query +=
                    @$"
                    INSERT OR {operation} INTO media_files('mapped_path', 'real_path', 'hash', 'source') VALUES('{hash.FilePath}', '{hash.RealPath}', '{hash.FileHash}', 'torrent');
                    " + Environment.NewLine; 
                }
            }
            return query;
        }

        public SqliteCommand[] ToMySQLCommands(SQLInsertMode sqlInsertMode = SQLInsertMode.replace, bool newFile = false)
        {
            string operation = sqlInsertMode == SQLInsertMode.ignore ? "IGNORE": "REPLACE";
            List<SqliteCommand> commandsBuffer = new();
            if(newFile)
            {
                foreach(var file in FilesList)
                {
                    var cmd = new SqliteCommand
                    {
                        CommandText = $"INSERT OR {operation} INTO media_files('real_path', 'source') VALUES($real_path, 'torrent')"
                    };
                    cmd.Parameters.AddWithValue("$real_path", file);
                    commandsBuffer.Add(cmd);
                }
            }
            else 
            {
                foreach(var hash in Hashes)
                {
                    var cmd = new SqliteCommand
                    {
                        CommandText = $"INSERT OR {operation} INTO media_files($file_path, $real_path, $hash, 'source') VALUES('$file_path', '$real_path', '$hash', 'torrent')"
                    };
                    cmd.Parameters.AddWithValue("$file_path", hash.FileHash);
                    cmd.Parameters.AddWithValue("$real_path", hash.RealPath);
                    cmd.Parameters.AddWithValue("$hash", hash.FileHash);
                    commandsBuffer.Add(cmd);
                }
            }
            return commandsBuffer.ToArray();
        }
    }
}