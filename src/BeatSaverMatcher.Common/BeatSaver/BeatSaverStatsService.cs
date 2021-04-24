using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Common.BeatSaver
{
    public class BeatSaverStatsService
    {
        private readonly BeatSaverRepository _beatSaverRepository;
        private readonly IDistributedCache _cache;
        private readonly ILogger<BeatSaverStatsService> _logger;

        public BeatSaverStatsService(BeatSaverRepository beatSaverRepository, IDistributedCache cache, ILogger<BeatSaverStatsService> logger)
        {
            _beatSaverRepository = beatSaverRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<BeatSaverStats> GetStats(int key)
        {

            var cached = await _cache.GetStringAsync(CacheKeys.GetForBeatmapStats(key));
            if (cached == null)
            {
                _logger.LogInformation("Loading Stats for Song 0x{SongKey:x}", key);
                BeatSaverSong song;
                try
                {
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20)))
                    {
                        song = await _beatSaverRepository.GetSong(key, cts.Token);
                    }
                }
                catch (TaskCanceledException)
                {
                    return null;
                }
                if (song == null)
                    return null;

                var options = new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromDays(1));
                await _cache.SetStringAsync(CacheKeys.GetForBeatmapStats(key), JsonConvert.SerializeObject(song.Stats), options);
                return song.Stats;
            }
            else
            {
                return JsonConvert.DeserializeObject<BeatSaverStats>(cached);
            }
        }
    }
}
