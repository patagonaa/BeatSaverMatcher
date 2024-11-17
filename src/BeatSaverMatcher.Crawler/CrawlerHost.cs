using BeatSaverMatcher.Common.BeatSaver;
using BeatSaverMatcher.Common.Db;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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

        private readonly HashSet<int> _keysInDb = new HashSet<int>();
        private static readonly TimeSpan _rescrapeTimeRange = TimeSpan.FromDays(30);

        public CrawlerHost(ILogger<CrawlerHost> logger, IBeatSaberSongRepository songRepository, BeatSaverRepository beatSaverRepository)
        {
            _logger = logger;
            _songRepository = songRepository;
            _beatSaverRepository = beatSaverRepository;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            uint i = 1; // don't do full scrape immediately
            while (true)
            {
                stoppingToken.ThrowIfCancellationRequested();
                await Scrape(stoppingToken, (i % 96) == 0);
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
                i++;
            }
        }

        private async Task Scrape(CancellationToken token, bool rescrape)
        {
            int startId;

            if (rescrape)
            {
                // scrape gaps in map keys sometimes, as it seems like since some time 2021
                // beatmaps can be uploaded, creating a key, without being published.
                // due to 404 in both cases, we can't know if the map was deleted or hasn't been published yet
                // so we just retry scraping missing maps for a month and hope nobody publishes maps after the rescrape time range

                startId = (await _songRepository.GetLatestBeatSaverKeyBefore(DateTime.UtcNow - _rescrapeTimeRange) ?? 0) + 1;
            }
            else
            {
                startId = (await _songRepository.GetLatestBeatSaverKey() ?? 0) + 1;
            }

            var endId = await _beatSaverRepository.GetLatestKey(token);
            _logger.LogInformation("Starting crawl at key {Key} {ScrapeType}", startId.ToString("x"), rescrape ? "(rescrape)" : "(latest)");
            for (int key = startId; key <= endId; key++)
            {
                token.ThrowIfCancellationRequested();

                if (_keysInDb.Contains(key))
                {
                    // keep track of keys already in db so we don't have to ask the database if we're scraping the time range again
                    _logger.LogDebug("Beatmap {Key} is already in DB!", key.ToString("x"));
                    continue;
                }

                if (await _songRepository.HasSong(key))
                {
                    _keysInDb.Add(key);
                    _logger.LogInformation("Beatmap {Key} is already in DB!", key.ToString("x"));
                    continue;
                }

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
                        _keysInDb.Add(key);

                        _logger.LogInformation("Inserted song {Key}: {SongName}", key.ToString("x"), mappedSong.Name);
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

                    if ((int)response.StatusCode < 200 || (int)response.StatusCode >= 300)
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
                CreatedAt = song.CreatedAt,
                UpdatedAt = song.UpdatedAt,
                LastPublishedAt = song.LastPublishedAt,
                Hash = BeatSaverUtils.MapHash(currentVersion.Hash),
                BeatSaverKey = int.Parse(song.Id, NumberStyles.HexNumber)
            };
        }
    }
}
