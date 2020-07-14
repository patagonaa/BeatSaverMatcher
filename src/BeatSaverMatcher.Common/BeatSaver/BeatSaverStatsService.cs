using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Common.BeatSaver
{
    public class BeatSaverStatsService
    {
        private readonly BeatSaverRepository _beatSaverRepository;
        private readonly IDistributedCache _cache;

        public BeatSaverStatsService(BeatSaverRepository beatSaverRepository, IDistributedCache cache)
        {
            _beatSaverRepository = beatSaverRepository;
            _cache = cache;
        }

        public async Task<BeatSaverStats> GetStats(int key)
        {

            var cached = await _cache.GetStringAsync(CacheKeys.GetForBeatmapStats(key));
            if (cached == null)
            {
                var song = await _beatSaverRepository.GetSong(key);
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
