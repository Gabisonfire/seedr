using System.Diagnostics;
using System.Runtime.CompilerServices;
using NLog;
using NLog.Config;  

namespace Seedr
{
    public static class Core
    {
        public static Logger logger = LogManager.GetCurrentClassLogger();
        public const string CONFIG_PATH = "settings.json";
        public static Utils.Config config = new();

        public static List<Torrent> torrentPool = new();
        public static List<Torrent> bufferTorrentPool = new();

        public static void Main(string[] args)
        {

            // Setup default logging
            var logConfig = new LoggingConfiguration();
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
            logConfig.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            logConfig.LoggingRules[0].EnableLoggingForLevel(LogLevel.Info);
            LogManager.Configuration = logConfig;

            // Load config
            config = Utils.ReadConfig();
            if (config.Debug)
            {
                logConfig.LoggingRules[0].EnableLoggingForLevel(LogLevel.Debug);
                LogManager.ReconfigExistingLoggers();
            }

            logger.Info("Seedr starting...");
            Database.InitDB();
            logger.Info("Connecting to torrent client...");
            ProcessTorrents();
        }

        static void ProcessTorrents()
        {
            //GetTorrentsFromClient();
            torrentPool.Add(
                new Torrent("test", "/home/gabisonfire/Downloads/lotr.zip")
            );
            // DEBUG
            HashAllTorrents();
            WriteHashes();
        }

        static void WriteHashes()
        {
            logger.Info("Writing hashes to database.");
            foreach(var torrent in torrentPool)
            {
                Database.Write(torrent.ToMySQL());
            }
        }

        static void GetTorrentsFromClient()
        {
            logger.Info("Fetching torrents from client...");
            switch(config.TorrentClient)
            {
                case "qbittorrent":
                Clients.Qbittorrent.FetchTorrents();
                break;
            }
        }

        static void HashAllTorrents()
        {
            bufferTorrentPool.Clear();
            var timer = new Stopwatch();
            logger.Info("Hashing torrents, depending on your system, this might take a while.");
            timer.Start();
            foreach(var torrent in torrentPool)
            {
                logger.Debug($"Hashing: {torrent.Name}");
                bufferTorrentPool.Add(Hashing.Hash(torrent));
            }
            foreach(var torrent in bufferTorrentPool)
            {
                logger.Debug($"Hashed torrent: {torrent}");
            }
            timer.Stop();
            logger.Info($"Hashing complete. Hashed {bufferTorrentPool.Count} torrents in {timer.Elapsed} using {config.HashAlgo}.");
            torrentPool.Clear();
            torrentPool.AddRange(bufferTorrentPool);
            bufferTorrentPool.Clear();
        }
    }
}