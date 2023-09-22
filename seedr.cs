using NLog;
using NLog.Config;

namespace Seedr
{
    public static class Core
    {
        public static Logger logger = LogManager.GetCurrentClassLogger();
        public const string CONFIG_PATH = "settings.json";
        public static Utils.Config config = new(CONFIG_PATH);

        public static void Main(string[] args)
        {

            // Setup default logging
            var logConfig = new LoggingConfiguration();
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
            logConfig.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            LogManager.Configuration = logConfig;

            // Load config
            config = Utils.ReadConfig(config.ConfigPath);
            Console.WriteLine(config.DownloadPath);

            logger.Info("Seedr starting...");
            Database.InitDB();
        }

        public static void Init()
        {
            
        }
    }
}