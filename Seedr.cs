using System.Data.HashFunction.xxHash;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Xml;
using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic;
using NLog;
using NLog.Config;
using NLog.LayoutRenderers;
using NLog.Layouts;
using Seedr.Utils;

namespace Seedr
{
    public static class Core
    {
        public static Logger logger = LogManager.GetCurrentClassLogger();
        public const string CONFIG_PATH = "settings.json";
        public static Config config = new();

        public static List<Torrent> torrentPool = new();
        public static List<LibraryFile> libraryFilesPool = new();

        public static void Main(string[] args)
        {

            // Setup default logging
            var logConfig = new LoggingConfiguration();
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole")
            {
                Layout = "${date} | ${uppercase:${level}} :: ${message}"
            };
            logConfig.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            logConfig.LoggingRules[0].EnableLoggingForLevel(LogLevel.Info);
            LogManager.Configuration = logConfig;

            // Load config
            config = Config.Read();
            if (config.Debug)
            {
                logConfig.LoggingRules[0].EnableLoggingForLevel(LogLevel.Debug);
                LogManager.ReconfigExistingLoggers();
            }

            logger.Info("Seedr starting...");
            Database.InitDB();
            logger.Info("Ready.");


            // DEBUGGING
            ProcessTorrents();
        }

        static void ProcessTorrents()
        {
            //RefreshTorrentsFromClient();
            // torrentPool.Add(
            //     new Torrent("lotr", "/home/gabisonfire/Downloads/lotr")
            // );
            // torrentPool.Add(
            //     new Torrent("lotr2", "/home/gabisonfire/Downloads/lotr.zip")
            // );
            // torrentPool.Add(
            //     new Torrent("lotr2", "/home/gabisonfire/Downloads/lotr2.zip")
            // );
            // torrentPool.Add(
            //     new Torrent("lotr3", "/home/gabisonfire/Downloads/lotr3.zip")
            // );
            torrentPool.Add(
                new Torrent("gf", "/mnt/torrents/seedr-test.mkv")
            );
            libraryFilesPool.Add(
                new LibraryFile("test", "/mnt/movies/movies/seedr-test-notogtitle.mkv")
            );
            // libraryFilesPool.Add(
            //     new LibraryFile("test", "/home/gabisonfire/Downloads/lotr2.zip")
            // );
            // libraryFilesPool.Add(
            //     new LibraryFile("test", "/home/gabisonfire/Downloads/lotr3.zip")
            // );
            // libraryFilesPool.Add(
            //     new LibraryFile("test", "/home/gabisonfire/Downloads/lotr4.zip")
            // );
            // DEBUG
            //HashAllTorrents();
            //RefreshFilesFromLibrary();
            //HashAllLibraryFiles();
            
            //SymLinkDupes();
            //FindNewFilesForHashing();
            HashNewFiles();
            
            
        }

        static void WriteHashes(List<IHashable> hashesToWrite)
        {
            logger.Info("Writing hashes to the database.");
            List<string> queries = new();
            foreach(var hash in hashesToWrite)
            {
                queries.Add(hash.ToMySQL());
            }
            Database.WriteMany(queries.ToArray());
        }

        // Rebuilds torrentPool with all current torrents.
        static void RefreshTorrentsFromClient()
        {
            logger.Info("Fetching torrents from client...");
            switch(config.TorrentClient)
            {
                case "qbittorrent":
                torrentPool = Clients.Qbittorrent.FetchTorrents();
                break;
            }
        }

        // Rebuild library pool from disk
        static void RefreshFilesFromLibrary()
        {
            libraryFilesPool.Clear();
            var allowedExtensions = config.ValidExtensions; 
            var files = Directory
                .GetFiles(config.LibraryPath)
                .Where(file => allowedExtensions.Any(file.ToLower().EndsWith))
                .ToList();
            foreach(var file in files)
            {
                libraryFilesPool.Add(new LibraryFile(file));
            }
            logger.Info($"Refreshed library and found {libraryFilesPool.Count()} file(s).");
        }

        static void HashAllTorrents()
        {
            Hashing.HashX(torrentPool, FileSource.Torrent);
        }

        static void HashAllLibraryFiles()
        {
            Hashing.HashX(libraryFilesPool, FileSource.Library);
        }

        // Find torrents removed from client, remove them from the database
        static HashValue[] FindRemovedTorrents()
        {
            var inDB = Database.ReadAllHashesFromDB(FileSource.Torrent);
            var inClient = Clients.Qbittorrent.FetchTorrents();
            var buffer = inDB.ToList();
            foreach(var hash in inDB)
            {
                foreach(var torrent in inClient)
                {
                    if(torrent.FilesList.Contains(hash.FilePath))
                    {
                        buffer.Remove(hash);
                    }
                }
            }
            foreach(var t in buffer)
            {
                Database.Delete(t.FileHash, t.FilePath);
            }
            return buffer.ToArray();
        }


        // Sym link duplicates found
        static void SymLinkDupes()
        {
            var dupes = Database.GetDuplicateHashes(FileSource.All);
            var pairs = dupes.GroupBy(x => x.FileHash)
            .Where(g => g.Count() > 1)
            .Select(y => y)
            .ToList();
            foreach(var groups in pairs)
            {
                var src = groups.ToList()[0].Source == FileSource.Torrent ? groups.ToList()[0]: groups.ToList()[1];
                var target = groups.ToList()[0].Source == FileSource.Library ? groups.ToList()[0]: groups.ToList()[1];
                File.Move(src.FilePath, $"{src.FilePath}.seedr.delete");
                File.CreateSymbolicLink(src.FilePath, target.FilePath);
                if(config.DeleteAfterLinking)
                {
                    File.Delete($"{src.FilePath}.seedr.delete");
                }
                //Console.WriteLine($"Create {src.FilePath}, targets {target.FilePath}");
            }
        }

        // Add files to db without a hash value
        static void FindNewFilesForHashing(string source = FileSource.All)
        {
            List<IHashable> fullBuffer = new();
            if(source == FileSource.All || source == FileSource.Library)
            {
                RefreshFilesFromLibrary();
                fullBuffer.AddRange(libraryFilesPool);
            }
            if(source == FileSource.All || source == FileSource.Library)
            {
                RefreshTorrentsFromClient();
                fullBuffer.AddRange(torrentPool);
            }
            List<SqliteCommand> commands = new();
            foreach(var file in fullBuffer)
            {
                commands.AddRange(file.ToMySQLCommands(SQLInsertMode.ignore, true));
            }
            Database.WriteCommands(commands.ToArray());
        }

        static void HashNewFiles()
        {
            List<IHashable> allFiles = Database.GetUnhashedFiles();
            foreach(var group in allFiles.GroupBy(x => x.FileType))
            {
                Hashing.HashX(group, group.Key);
            }
            
        }
    }
}