using BeatSaverMatcher.Common.BeatSaver;
using BeatSaverMatcher.Common.Db;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Crawler
{
    class CrawlerHost : BackgroundService
    {
        private readonly ILogger<CrawlerHost> _logger;
        private readonly IBeatSaberSongRepository _songRepository;
        private readonly BeatSaverRepository _beatSaverRepository;
        private readonly Counter _currentSongId = Metrics.CreateCounter("beatsaver_latest_song_id", "ID of the song that was most recently scraped", new CounterConfiguration { SuppressInitialValue = true });

        public CrawlerHost(ILogger<CrawlerHost> logger, IBeatSaberSongRepository songRepository, BeatSaverRepository beatSaverRepository)
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
            var lastScraped = await _songRepository.GetLatestBeatSaverKey() ?? 0;

            _currentSongId.IncTo(lastScraped);

            var startId = lastScraped + 1;

            var endId = await _beatSaverRepository.GetLatestKey(token);
            _logger.LogInformation("Starting crawl at key {Key}", startId.ToString("x"));
            for (int key = startId; key <= endId; key++)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    var song = await _beatSaverRepository.GetSong(key, token);
                    if (song == null)
                    {
                        _logger.LogInformation("Beatmap {Key} not found!", key.ToString("x"));
                        continue;
                    }

                    var mappedSong = MapSong(song);
                    if (mappedSong != null)
                    {
                        await _songRepository.InsertSong(mappedSong);

                        _logger.LogInformation("Inserted song {Key}: {SongName}", key.ToString("x"), mappedSong.Name);
                        _currentSongId.IncTo(key);
                    }
                }
                catch (WebException wex)
                {
                    var response = wex.Response as HttpWebResponse;
                    if (response == null)
                    {
                        _logger.LogWarning(wex, "Unknown WebException");
                        break;
                    }

                    if ((int)response.StatusCode < 200 && (int)response.StatusCode >= 300)
                    {
                        _logger.LogWarning("Error {StatusCode} - {StatusDescription} while scraping", response.StatusCode, response.StatusDescription);
                        break;
                    }

                    _logger.LogWarning(wex, "Unknown Exception");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unknown Exception");
                    break;
                }
            }

            _logger.LogInformation("Scrape done.");
        }

        private BeatSaberSong MapSong(BeatSaverSong song)
        {
            var currentVersion = song.Versions
                .Where(x => x.State == "Published")
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();

            if (currentVersion == null)
                return null;

            return new BeatSaberSong
            {
                LevelAuthorName = song.Metadata.LevelAuthorName,
                SongAuthorName = song.Metadata.SongAuthorName,
                SongName = song.Metadata.SongName,
                SongSubName = song.Metadata.SongSubName,
                Bpm = song.Metadata.Bpm,
                Name = song.Name,
                AutoMapper = song.Automapper ? song.Metadata.LevelAuthorName : null,
                Difficulties = BeatSaverUtils.MapDifficulties(currentVersion.Diffs),
                Uploader = song.Uploader.Name,
                Uploaded = song.Uploaded,
                Hash = BeatSaverUtils.MapHash(currentVersion.Hash),
                BeatSaverKey = int.Parse(song.Id, NumberStyles.HexNumber)
            };
        }
    }
}
