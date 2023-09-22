using System.Security;
using Microsoft.VisualBasic;
using QBittorrent;
using QBittorrent.Client;
using Seedr;
using System.Linq;

namespace Clients
{
    public class Qbittorrent
    {
        static readonly Uri uri = new(Core.config.TorrentClientUrl);
        static QBittorrentClient qbt = new(uri);

        public static void FetchTorrents()
        {
            var fetch = qbt.GetTorrentListAsync();
            fetch.Wait();
            IReadOnlyList<TorrentInfo> torrents = fetch.Result;
            Core.torrentPool.Clear();
            foreach(var torrent in torrents)
            {
                if(!ValidState(torrent.State)) { continue; } // Skip the torrent if not in a valid state
                Core.torrentPool.Add(
                    new Torrent(
                        torrent.Name,
                        torrent.ContentPath
                    )
                );
            }        
        }

        public static bool ValidState(TorrentState State)
        {
            TorrentState[] validStates = {
                TorrentState.ForcedUpload,
                TorrentState.PausedUpload,
                TorrentState.QueuedUpload,
                TorrentState.Uploading,
                TorrentState.StalledUpload
            };
            if(validStates.Contains(State))
            {
                return true;
            }
            return false;
        }
    }
}