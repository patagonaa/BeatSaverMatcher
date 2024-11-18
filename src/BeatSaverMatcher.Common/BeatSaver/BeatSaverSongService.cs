using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Common.BeatSaver
{
    public class BeatSaverSongService
    {
        private readonly BeatSaverRepository _beatSaverRepository;
        private readonly IDistributedCache _cache;
        private readonly ILogger<BeatSaverSongService> _logger;
        private const string NegativeCacheEntryValue = "[SongNotExisting]";

        public BeatSaverSongService(BeatSaverRepository beatSaverRepository, IDistributedCache cache, ILogger<BeatSaverSongService> logger)
        {
            _beatSaverRepository = beatSaverRepository;
            _cache = cache;
            _logger = logger;
        }


        public async Task<IDictionary<int, BeatSaverSong>> GetSongs(IList<int> keys, Action<int, int> progressCallback, CancellationToken cancellationToken)
        {
            var toReturn = new Dictionary<int, BeatSaverSong>();

            progressCallback(0, keys.Count);

            var progress = 0;

            var toFetch = new List<int>();
            const int fetchBatchSize = 50;
            foreach (var key in keys)
            {
                var (wasCached, song) = await GetFromCache(key, cancellationToken);
                if (wasCached)
                {
                    toReturn[key] = song;
                    progress++;
                    progressCallback(progress, keys.Count);
                }
                else
                {
                    toFetch.Add(key);
                }
            }

            _logger.LogInformation("Got {Keys} / {TotalKeys} Maps (Cache).", progress, keys.Count);

            foreach (var batch in toFetch.Chunk(fetchBatchSize))
            {
                IDictionary<string, BeatSaverSong> songs;
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(20));
                    songs = await _beatSaverRepository.GetSongs(batch, cts.Token);
                }
                catch (TaskCanceledException)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw;
                    throw new TimeoutException();
                }

                foreach (var key in batch)
                {
                    // iterate batch instead of fetch result so we can add negative cache entries for missing/deleted maps
                    var song = songs.TryGetValue(key.ToString("x"), out var tmp) ? tmp : null;
                    await UpdateSongCache(key, song, cancellationToken);
                    if (song != null)
                        toReturn[key] = song;
                }

                progress += batch.Length;
                progressCallback(progress, keys.Count);
                _logger.LogInformation("Got {Keys} / {TotalKeys} Maps (BeatSaver).", progress, keys.Count);
            }

            return toReturn;
        }

        private async Task<(bool, BeatSaverSong)> GetFromCache(int key, CancellationToken cancellationToken)
        {
            var cached = await _cache.GetStringAsync(CacheKeys.GetForBeatmap(key), cancellationToken);
            if (cached != null)
            {
                if (cached == NegativeCacheEntryValue)
                {
                    _logger.LogDebug("Negative cache entry for song 0x{SongKey:x} in cache", key);
                    return (true, null);
                }

                _logger.LogDebug("Got song 0x{SongKey:x} from cache", key);
                return (true, JsonSerializer.Deserialize<BeatSaverSong>(cached));
            }
            return (false, null);
        }

        public async Task<BeatSaverSong> GetSong(int key, CancellationToken cancellationToken)
        {
            var cached = await _cache.GetStringAsync(CacheKeys.GetForBeatmap(key), cancellationToken);
            if (cached != null)
            {
                if (cached == NegativeCacheEntryValue)
                {
                    _logger.LogDebug("Negative cache entry for song 0x{SongKey:x} in cache", key);
                    return null;
                }

                _logger.LogDebug("Got song 0x{SongKey:x} from cache", key);
                return JsonSerializer.Deserialize<BeatSaverSong>(cached);
            }

            _logger.LogInformation("Loading song 0x{SongKey:x}", key);
            BeatSaverSong song;
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(20));
                song = await _beatSaverRepository.GetSong(key, cts.Token);
            }
            catch (TaskCanceledException)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;
                throw new TimeoutException();
            }

            await UpdateSongCache(key, song, cancellationToken);
            return song;
        }

        public async Task UpdateSongCache(int key, BeatSaverSong song, CancellationToken cancellationToken)
        {
            TimeSpan cacheTime;
            if (song == null)
            {
                cacheTime = TimeSpan.FromDays(1);
            }
            else if (song.Uploaded < (DateTime.UtcNow - TimeSpan.FromDays(365)))
            {
                cacheTime = TimeSpan.FromDays(30);
            }
            else if (song.Uploaded < (DateTime.UtcNow - TimeSpan.FromDays(90)))
            {
                cacheTime = TimeSpan.FromDays(14);
            }
            else if (song.Uploaded < (DateTime.UtcNow - TimeSpan.FromDays(30)))
            {
                cacheTime = TimeSpan.FromDays(7);
            }
            else
            {
                cacheTime = TimeSpan.FromDays(1);
            }

            var options = new DistributedCacheEntryOptions().SetAbsoluteExpiration(cacheTime);
            await _cache.SetStringAsync(CacheKeys.GetForBeatmap(key), song == null ? NegativeCacheEntryValue : JsonSerializer.Serialize(song), options, cancellationToken);
        }
    }
}
