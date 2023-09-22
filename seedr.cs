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
            FetchTorrents();
        }

        public static void FetchTorrents()
        {
            logger.Info("Fetching torrents...");
            switch(config.TorrentClient)
            {
                case "qbittorrent":
                Clients.Qbittorrent.FetchTorrents();
                break;
            }
            foreach(var t in torrentPool)
            {
                Console.WriteLine(t.ToString());
                Hashing.GetChecksum(t.Path);
                break;
            }
        }
    }
}