using BeatSaverMatcher.Common.Db;
using Microsoft.Extensions.Hosting;
using Prometheus;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Crawler
{
    internal class MetricsScrapeHost : IHostedService
    {
        private readonly Gauge _totalBeatmaps = Metrics.CreateGauge("beatsaver_beatmaps_total", "Total number of beatmaps", new GaugeConfiguration { SuppressInitialValue = true, LabelNames = new[] { "automapper" }  });
        private readonly Gauge _totalBeatmapsPerDifficulty = Metrics.CreateGauge("beatsaver_beatmaps_per_difficulty_total", "Total number of beatmaps by difficulty", new GaugeConfiguration { SuppressInitialValue = true, LabelNames = new[] { "automapper", "difficulty" }  });
        private readonly Counter _currentSongId = Metrics.CreateCounter("beatsaver_latest_song_id", "ID of the song that was most recently scraped", new CounterConfiguration { SuppressInitialValue = true });

        private readonly IBeatSaberSongRepository _songRepository;

        public MetricsScrapeHost(IBeatSaberSongRepository songRepository)
        {
            _songRepository = songRepository;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Metrics.DefaultRegistry.AddBeforeCollectCallback(GetMetrics);
            return Task.CompletedTask;
        }

        private async Task GetMetrics(CancellationToken cancellationToken)
        {
            var lastScraped = await _songRepository.GetLatestBeatSaverKey() ?? 0;
            _currentSongId.IncTo(lastScraped);

            var songGroups = await _songRepository.GetSongCount();

            _totalBeatmaps.WithLabels("true").Set(songGroups.Where(x => x.AutoMapper).Sum(x => x.Count));
            _totalBeatmaps.WithLabels("false").Set(songGroups.Where(x => !x.AutoMapper).Sum(x => x.Count));

            var difficulties = Enum.GetValues<SongDifficulties>();
            foreach (var difficulty in difficulties)
            {
                var difficultyBeatmaps = songGroups.Where(x => x.Difficulties.HasFlag(difficulty)).ToList();
                _totalBeatmapsPerDifficulty.WithLabels("true", difficulty.ToString()).Set(difficultyBeatmaps.Where(x => x.AutoMapper).Sum(x => x.Count));
                _totalBeatmapsPerDifficulty.WithLabels("false", difficulty.ToString()).Set(difficultyBeatmaps.Where(x => !x.AutoMapper).Sum(x => x.Count));
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
