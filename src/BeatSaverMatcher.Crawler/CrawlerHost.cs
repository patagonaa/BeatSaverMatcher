using BeatSaverMatcher.Common.BeatSaver;
using BeatSaverMatcher.Common.Db;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Crawler
{
    sealed class CrawlerHost : BackgroundService
    {
        private readonly ILogger<CrawlerHost> _logger;
        private readonly IBeatSaberSongRepository _songRepository;
        private readonly BeatSaverRepository _beatSaverRepository;

        private static readonly TaskCompletionSource _firstRunFinishedTcs = new();
        public static readonly Task FirstRunFinished = _firstRunFinishedTcs.Task;

        public CrawlerHost(ILogger<CrawlerHost> logger,
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
            DateTime lastUpdatedAt = DateTime.UnixEpoch;

            _logger.LogInformation("Starting update crawl at {Date}", lastUpdatedAt);

            while (true)
            {
                token.ThrowIfCancellationRequested();

                IList<BeatSaverSong> songs;
                try
                {
                    songs = await _beatSaverRepository.GetSongsUpdatedAfter(lastUpdatedAt, token);
                    _logger.LogInformation("Got {SongCount} songs from BeatSaver", songs.Count);
                }
                catch (HttpRequestException wex)
                {
                    if (wex.StatusCode.HasValue && ((int)wex.StatusCode < 200 || (int)wex.StatusCode >= 300))
                    {
                        _logger.LogWarning("Error {StatusCode} while scraping", (int)wex.StatusCode);
                        break;
                    }

                    _logger.LogWarning(wex, "Unknown Exception while fetching");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unknown Exception while fetching");
                    break;
                }

                if (songs.Count > 0 && songs.Last().UpdatedAt <= lastUpdatedAt)
                {
                    _logger.LogError("Beatsaver returned map before or at last update date, this would cause an endless loop!");
                    break;
                }

                foreach (var song in songs.Reverse())
                {
                    try
                    {
                        token.ThrowIfCancellationRequested();
                        await InsertOrUpdate(song, token);

                        if (song.UpdatedAt < lastUpdatedAt)
                        {
                            throw new Exception("Song list must be sorted ascending by UpdatedAt");
                        }
                        else
                        {
                            lastUpdatedAt = song.UpdatedAt;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Unknown Exception while updating song {Key}", song.Id);
                        break;
                    }
                }

                if (songs.Count == 0)
                {
                    break;
                }
            }

            _firstRunFinishedTcs.TrySetResult();
            _logger.LogInformation("Scrape done.");
        }

        private async Task InsertOrUpdate(BeatSaverSong song, CancellationToken cancellationToken)
        {
            var mappedSong = MapSong(song);
            if (mappedSong != null)
            {
                var inserted = await _songRepository.InsertOrUpdateSong(mappedSong);

                _logger.LogInformation("{Action} song {Key}: {SongName}", inserted ? "Inserted" : "Updated", mappedSong.BeatSaverKey.ToString("x"), mappedSong.Name);
            }
        }

        private BeatSaberSong? MapSong(BeatSaverSong song)
        {
            var currentVersion = song.Versions
                .Where(x => x.State == "Published")
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();

            if (currentVersion == null)
                return null;

            return new BeatSaberSong
            {
                LevelAuthorName = LimitLength(song.Metadata.LevelAuthorName, 4000),
                SongAuthorName = LimitLength(song.Metadata.SongAuthorName, 4000),
                SongName = LimitLength(song.Metadata.SongName, 4000),
                SongSubName = LimitLength(song.Metadata.SongSubName, 4000),
                Bpm = song.Metadata.Bpm,
                Name = LimitLength(song.Name, 4000),
                AutoMapper = song.Automapper,
                Difficulties = BeatSaverUtils.MapDifficulties(currentVersion.Diffs),
                Uploader = LimitLength(song.Uploader.Name, 4000),
                Uploaded = song.Uploaded ?? song.CreatedAt,
                CreatedAt = song.CreatedAt,
                UpdatedAt = song.UpdatedAt,
                LastPublishedAt = song.LastPublishedAt,
                Hash = BeatSaverUtils.MapHash(currentVersion.Hash),
                BeatSaverKey = int.Parse(song.Id, NumberStyles.HexNumber)
            };
        }

        [return: NotNullIfNotNull(nameof(a))]
        private static string? LimitLength(string? a, int length)
        {
            if (a == null || a.Length <= length)
                return a;
            return string.Concat(a.AsSpan(0, length - 3), "...");
        }
    }
}
