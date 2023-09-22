using System.Text.Json;
using System.Text.Json.Serialization;
using NLog;

namespace Seedr
{
    public static class Utils
    {
        public class Config
        {
            public string ConfigPath {get;}
            [JsonPropertyName("library_path")]
            public string LibraryPath {get; set;} = string.Empty;
            [JsonPropertyName("download_path")]
            public string DownloadPath {get; set;} = string.Empty;

            public Config(string ConfigPath)
            {
                this.ConfigPath = ConfigPath;
            }
        }

        public static Config ReadConfig(string ConfigPath)
        {
            if(!File.Exists(ConfigPath))
            {
                Core.logger.Error($"Required \"{ConfigPath}\" could not be found.");
                Environment.Exit(1);
            }
            try 
            {
                var cfg = JsonSerializer.Deserialize<Config>(File.ReadAllText(ConfigPath));
                if (cfg != null)
                {
                    return cfg;
                }
                else
                {
                    Core.logger.Debug($"{ConfigPath} ended up null.");
                    Core.logger.Error($"{ConfigPath} could not be properly read.");
                    Environment.Exit(1);
                }
            }
            catch(Exception e)
            {
                Core.logger.Error($"An error occured reading {ConfigPath}, {e.Message}");
                Environment.Exit(1);
            }
            return null;
        }
    }
}