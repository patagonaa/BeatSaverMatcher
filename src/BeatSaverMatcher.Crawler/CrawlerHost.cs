﻿using BeatSaverMatcher.Common;
using BeatSaverMatcher.Common.BeatSaver;
using BeatSaverMatcher.Common.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace BeatSaverMatcher.Crawler
{
    class CrawlerHost : IHostedService
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Timer _timer;
        private readonly ILogger<CrawlerHost> _logger;
        private readonly IBeatSaberSongRepository _songRepository;
        private readonly BeatSaverRepository _beatSaverRepository;
        private readonly SemaphoreSlim _scrapeLock = new SemaphoreSlim(1, 1);
        private readonly Counter _currentSongId = Metrics.CreateCounter("beatsaver_latest_song_id", "ID of the song that was most recently scraped", new CounterConfiguration { SuppressInitialValue = true });

        public CrawlerHost(ILogger<CrawlerHost> logger, IBeatSaberSongRepository songRepository, BeatSaverRepository beatSaverRepository)
        {
            _timer = new Timer
            {
                Interval = 15 * 60 * 1000,
                AutoReset = true
            };
            _timer.Elapsed += (sender, e) => Worker();
            _logger = logger;
            _songRepository = songRepository;
            _beatSaverRepository = beatSaverRepository;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => _cts.Cancel());
            _ = Task.Run(() => Worker()).ContinueWith(x => _timer.Start());
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer.Stop();
            return Task.CompletedTask;
        }

        private async Task Worker()
        {
            try
            {
                await _scrapeLock.WaitAsync();

                var lastScraped = await _songRepository.GetLatestBeatSaverKey() ?? 0;

                _currentSongId.IncTo(lastScraped);

                var startId = lastScraped + 1;

                var endId = await _beatSaverRepository.GetLatestKey(_cts.Token);
                _logger.LogInformation("Starting crawl at key {Key}", startId.ToString("x"));
                for (int key = startId; key <= endId; key++)
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    try
                    {
                        var song = await _beatSaverRepository.GetSong(key, _cts.Token);
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
            finally
            {
                _scrapeLock.Release();
            }
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
                Difficulties = MapDifficulties(currentVersion.Diffs),
                Uploader = song.Uploader.Name,
                Uploaded = song.Uploaded,
                Hash = MapHash(currentVersion.Hash),
                BeatSaverKey = int.Parse(song.Id, NumberStyles.HexNumber)
            };
        }

        private byte[] MapHash(string hash)
        {
            byte[] toReturn = new byte[hash.Length / 2];
            for (int i = 0; i < hash.Length; i += 2)
                toReturn[i / 2] = Convert.ToByte(hash.Substring(i, 2), 16);
            return toReturn;
        }

        private SongDifficulties MapDifficulties(IList<BeatSaverDifficulty> difficulties)
        {
            SongDifficulties toReturn = 0;
            if (difficulties.Any(x => x.Difficulty == BeatSaverDifficultyType.Easy))
                toReturn |= SongDifficulties.Easy;
            if (difficulties.Any(x => x.Difficulty == BeatSaverDifficultyType.Normal))
                toReturn |= SongDifficulties.Normal;
            if (difficulties.Any(x => x.Difficulty == BeatSaverDifficultyType.Hard))
                toReturn |= SongDifficulties.Hard;
            if (difficulties.Any(x => x.Difficulty == BeatSaverDifficultyType.Expert))
                toReturn |= SongDifficulties.Expert;
            if (difficulties.Any(x => x.Difficulty == BeatSaverDifficultyType.ExpertPlus))
                toReturn |= SongDifficulties.ExpertPlus;
            return toReturn;
        }
    }
}
