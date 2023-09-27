using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Xml;
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
            torrentPool.Add(
                new Torrent("lotr", "/home/gabisonfire/Downloads/lotr")
            );
            torrentPool.Add(
                new Torrent("lotr2", "/home/gabisonfire/Downloads/lotr.zip")
            );
            torrentPool.Add(
                new Torrent("lotr2", "/home/gabisonfire/Downloads/lotr2.zip")
            );
            torrentPool.Add(
                new Torrent("lotr3", "/home/gabisonfire/Downloads/lotr3.zip")
            );
            torrentPool.Add(
                new Torrent("lotr4", "/home/gabisonfire/Downloads/lotr4.zip")
            );
            // DEBUG
            //HashAllTorrents();
            //RefreshFilesFromLibrary();
            //HashAllLibraryFiles();
            var h = Database.FindDuplicates();
            // WriteHashes(libraryFilesPool.ToList<IHashable>());
            // foreach (var hash in Database.ReadAllHashesFromDB("library"))
            // {
            //     Console.WriteLine(hash);
            // }
            // var removed = FindRemovedTorrents();
            // Console.WriteLine(removed.Length);
            Database.MarkForDeletion(torrentPool[0].Hashes.First().FileHash);
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
            Hashing.HashX(torrentPool);
        }

        static void HashAllLibraryFiles()
        {
            // DEBUG
            libraryFilesPool = libraryFilesPool.GetRange(0,3);
            //
            Hashing.HashX(libraryFilesPool);
        }

        static HashValue[] FindRemovedTorrents()
        {
            var inDB = Database.ReadAllHashesFromDB("torrent");
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
            return buffer.ToArray();
        }
    }
}