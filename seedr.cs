using System.Diagnostics;
using System.Runtime.CompilerServices;
using NLog;
using NLog.Config;
using NLog.Layouts;
using Seedr.Utils;

namespace Seedr
{
    public static class Core
    {
        public static Logger logger = LogManager.GetCurrentClassLogger();
        public const string CONFIG_PATH = "settings.json";
        public static Utils.Config config = new();

        public static List<Torrent> torrentPool = new();
        public static List<Torrent> bufferTorrentPool = new();

        public static List<Task> taskPool = new();

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
            config = Utils.Config.Read();
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
                new Torrent("lotr", "/home/gabisonfire/Downloads/lotr.zip")
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
            /* 
            Clear bufferpool. We use a buffer pool because it's easier to just overwrite the original pool once all torrents were hashed.
            */
            bufferTorrentPool.Clear(); 
            var timer = new Stopwatch();
            logger.Info("Hashing torrents, depending on your system, this might take a while.");
            timer.Start();
            // If the division equals zero, ex: 4 torrent but 10 threads allowed, we set stack size to 1 (all torrents in a single stack)
            // which leads to all torrent being hashed on a seperate task.
            int stackSize = torrentPool.Count()/config.HashingThreads < 1 ? 1 : torrentPool.Count()/config.HashingThreads;
            var stacks = torrentPool.Chunk(stackSize);
            foreach(var pool in stacks)
            {
                Task t = Task.Factory.StartNew(() => HashX(pool), TaskCreationOptions.AttachedToParent);
                taskPool.Add(t);
            }

            // Wait for all threads to complete
            Task.WaitAll(taskPool.ToArray());

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

        static void HashX(IEnumerable<Torrent> pool)
        {
            foreach(var torrent in pool)
            {
                bufferTorrentPool.Add(Hashing.Hash(torrent));
            }
        }
    }
}