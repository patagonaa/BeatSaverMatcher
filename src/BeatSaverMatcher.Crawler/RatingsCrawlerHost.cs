using BeatSaverMatcher.Common.BeatSaver;
using BeatSaverMatcher.Common.Db;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Crawler
{
    class RatingsCrawlerHost : BackgroundService
    {
        private readonly ILogger<RatingsCrawlerHost> _logger;
        private readonly IBeatSaberSongRepository _songRepository;
        private readonly BeatSaverRepository _beatSaverRepository;

        public RatingsCrawlerHost(ILogger<RatingsCrawlerHost> logger,
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
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }

        private async Task Scrape(CancellationToken token)
        {
            var lastUpdatedAt = await _songRepository.GetLatestScoreUpdatedAt(token) ?? DateTime.UnixEpoch;
            _logger.LogInformation("Starting ratings crawl at {Key}", lastUpdatedAt);

            var now = DateTime.UtcNow;
            var scores = await _beatSaverRepository.GetScoresAfter(lastUpdatedAt, token);

            foreach (var score in scores)
            {
                token.ThrowIfCancellationRequested();
                var rating = new BeatSaberSongRatings
                {
                    BeatSaverKey = score.MapId,
                    Score = score.Score,
                    Downvotes = score.Downvotes,
                    Upvotes = score.Upvotes,
                    UpdatedAt = now // technically this isn't 100% safe. if this crashes, we miss some votes but whatever
                };

                var inserted = await _songRepository.InsertOrUpdateSongRatings(rating);
                _logger.LogInformation("{Action} score for 0x{Key}", inserted ? "Inserted" : "Updated", score.MapId.ToString("x"));
            }
            _logger.LogInformation("Ratings scrape done.");
        }
    }
}
