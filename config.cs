using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using NLog;

namespace Seedr.Utils
{
    public partial class Remapper
    {
        [JsonPropertyName("path")]
        public string Path {get; set;} = string.Empty;
        [JsonPropertyName("remap_path")]
        public string RemapPath {get; set;} = string.Empty;
    }

    public partial class Config
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
        [JsonPropertyName("hashing_threads")]
        public int HashingThreads { get; set; }
        [JsonPropertyName("valid_extensions")]
        public string[] ValidExtensions { get; set; } = new string[]{};
        [JsonPropertyName("exclude_torrent_path")]
        public string[] ExcludeTorrentPath { get; set; } = new string[]{};
        [JsonPropertyName("torrent_path_remappers")]
        public Remapper[] PathRemappers { get; set; } = new Remapper[]{};
        [JsonPropertyName("delete_after_linking")]
        public bool DeleteAfterLinking {get; set;}

        public void WriteConfig()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var cfg = JsonSerializer.Serialize(this, options);
            Core.logger.Debug($"Writing config {Core.CONFIG_PATH}");
            try {
                File.WriteAllText(Core.CONFIG_PATH, cfg);
            }
            catch(Exception e)
            {
                Core.logger.Error(e.Message);
            }
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

        public static Config Read(string ConfigPath = Core.CONFIG_PATH)
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

        /* 
        Remap function on all paths for cases where the client and app sit on a different
        server than the data host.
        */
        public static string Remap(string filePath, bool reverse = false)
        {
            foreach(var remap in Core.config.PathRemappers)
            {
                Core.logger.Debug($"Remapping {filePath}");
                if(reverse)
                {
                    filePath = filePath.Replace(remap.RemapPath, remap.Path);
                }
                else 
                {
                    filePath = filePath.Replace(remap.Path, remap.RemapPath);
                }
                Core.logger.Debug($"remapped to {filePath}");   
            }
            return filePath;
        }
    }

    public static class FileSource
    {
        public const string Torrent = "torrent";
        public const string Library = "library";
        public const string All = "all";
    }
}