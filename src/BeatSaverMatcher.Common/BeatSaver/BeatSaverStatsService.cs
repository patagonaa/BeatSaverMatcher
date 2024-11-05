﻿using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Common.BeatSaver
{
    public class BeatSaverStatsService
    {
        private readonly BeatSaverRepository _beatSaverRepository;
        private readonly IDistributedCache _cache;
        private readonly ILogger<BeatSaverStatsService> _logger;
        private const string NegativeCacheEntryValue = "[SongNotExisting]";

        public BeatSaverStatsService(BeatSaverRepository beatSaverRepository, IDistributedCache cache, ILogger<BeatSaverStatsService> logger)
        {
            _beatSaverRepository = beatSaverRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<BeatSaverStats> GetStats(int key, CancellationToken cancellationToken)
        {
            var cached = await _cache.GetStringAsync(CacheKeys.GetForBeatmapStats(key), cancellationToken);
            if (cached != null)
            {
                if (cached == NegativeCacheEntryValue)
                {
                    _logger.LogDebug("Negative cache entry for song 0x{SongKey:x} in cache", key);
                    return null;
                }

                _logger.LogDebug("Got stats for song 0x{SongKey:x} from cache", key);
                return JsonSerializer.Deserialize<BeatSaverStats>(cached);
            }

            _logger.LogInformation("Loading stats for song 0x{SongKey:x}", key);
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

            TimeSpan cacheTime;
            if (song == null)
            {
                cacheTime = TimeSpan.FromDays(30);
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
            await _cache.SetStringAsync(CacheKeys.GetForBeatmapStats(key), song == null ? NegativeCacheEntryValue : JsonSerializer.Serialize(song.Stats), options, cancellationToken);
            return song?.Stats;
        }
    }
}
