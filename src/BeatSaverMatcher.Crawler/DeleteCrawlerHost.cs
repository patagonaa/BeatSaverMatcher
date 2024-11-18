using BeatSaverMatcher.Common.BeatSaver;
using BeatSaverMatcher.Common.Db;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Crawler
{
    class DeleteCrawlerHost : BackgroundService
    {
        private readonly ILogger<DeleteCrawlerHost> _logger;
        private readonly IBeatSaberSongRepository _songRepository;
        private readonly BeatSaverRepository _beatSaverRepository;

        public DeleteCrawlerHost(ILogger<DeleteCrawlerHost> logger,
            IBeatSaberSongRepository songRepository,
            BeatSaverRepository beatSaverRepository)
        {
            _logger = logger;
            _songRepository = songRepository;
            _beatSaverRepository = beatSaverRepository;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (true)
            {
                stoppingToken.ThrowIfCancellationRequested();
                await Scrape(stoppingToken);
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task Scrape(CancellationToken token)
        {
            var keys = await _songRepository.GetAllKeys(token);
            _logger.LogInformation("Checking {Count} maps for deletion", keys.Count);

            foreach (var batch in keys.Chunk(50))
            {
                var maps = await _beatSaverRepository.GetSongs(batch, token);
                _logger.LogInformation("Checking next delete batch");
                foreach (var key in batch)
                {
                    if (!maps.ContainsKey(key.ToString("x")))
                    {
                        _logger.LogInformation("Marking 0x{Key} as deleted", key.ToString("x"));
                        await _songRepository.MarkDeleted(key, DateTime.UtcNow);
                    }
                }

                await Task.Delay(1000, token);
            }
            _logger.LogInformation("Delete scrape done.");
        }
    }
}
