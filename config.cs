using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using NLog;

namespace Seedr
{
    public static class Utils
    {
        public class Config
        {
            public static readonly string[] ALLOWED_CLIENTS = {"transmission", "qbittorrent"};
            public static readonly string[] SUPPORTED_HASH = {"crc32", "md5", "md5_invoked", "xxhash64"};

            [JsonPropertyName("library_path")]
            public string LibraryPath {get; set;} = string.Empty;
            [JsonPropertyName("download_path")]
            public string DownloadPath {get; set;} = string.Empty;
            [JsonPropertyName("debug")]
            public bool Debug {get; set;}
            [JsonPropertyName("torrent_client")]
            public string TorrentClient { get; set; } = string.Empty;
            [JsonPropertyName("torrent_client_url")]
            public string TorrentClientUrl { get; set; } = string.Empty;
            [JsonPropertyName("hash_algo")]
            public string HashAlgo { get; set; } = string.Empty;

            public void WriteConfig()
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var cfg = JsonSerializer.Serialize(this, options);
                File.WriteAllText(Core.CONFIG_PATH, cfg);
            }
            public void Validate()
            {
                if(!ALLOWED_CLIENTS.Any(TorrentClient.Equals))
                {
                    Core.logger.Error($"{TorrentClient} is not supported.");
                    Environment.Exit(1);
                }
                if(!SUPPORTED_HASH.Any(HashAlgo.Equals))
                {
                    Core.logger.Error($"{HashAlgo} is not supported.");
                    Environment.Exit(1);
                }
            }

            public void Sanitize()
            {
                TorrentClient =  TorrentClient.ToLower();
            }
        }

        public static Config ReadConfig(string ConfigPath = Core.CONFIG_PATH)
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
                    cfg.Sanitize();
                    cfg.Validate();
                    return cfg;
                }
                else
                {
                    Core.logger.Debug($"{ConfigPath} ended up null.");
                    Core.logger.Error($"{ConfigPath} could not be properly read. Verify syntax.");
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