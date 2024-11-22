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
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }

        private async Task Scrape(CancellationToken token)
        {
            var latestDeleted = await _songRepository.GetLatestDeletedAt(token) ?? DateTime.UnixEpoch;
            _logger.LogInformation("Starting delete crawl at {Date}", latestDeleted);

            while (true)
            {
                token.ThrowIfCancellationRequested();

                IList<BeatSaverDeletedSong> songs;
                try
                {
                    songs = await _beatSaverRepository.GetSongsDeletedAfter(latestDeleted, token);
                    _logger.LogInformation("Got {SongCount} deleted songs from BeatSaver", songs.Count);
                }
                catch (WebException wex)
                {
                    if (wex.Response is HttpWebResponse response)
                    {
                        if ((int)response.StatusCode < 200 || (int)response.StatusCode >= 300)
                        {
                            _logger.LogWarning("Error {StatusCode} - {StatusDescription} while scraping", response.StatusCode, response.StatusDescription);
                            break;
                        }

                        _logger.LogWarning(wex, "Unknown Exception while fetching");
                        break;
                    }
                    else
                    {
                        _logger.LogWarning(wex, "Unknown WebException");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unknown Exception while fetching");
                    break;
                }

                if (songs.Count > 0 && songs.Last().DeletedAt <= latestDeleted)
                {
                    _logger.LogError("Beatsaver returned map before or at last update date, this would cause an endless loop!");
                    break;
                }

                try
                {
                    foreach (var song in songs.Reverse())
                    {
                        token.ThrowIfCancellationRequested();
                        await _songRepository.MarkDeleted(int.Parse(song.Id, NumberStyles.HexNumber), song.DeletedAt);

                        if (song.DeletedAt < latestDeleted)
                        {
                            throw new Exception("Song list must be sorted ascending by DeletedAt");
                        }
                        else
                        {
                            latestDeleted = song.DeletedAt;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unknown Exception");
                    break;
                }

                if (songs.Count == 0)
                {
                    break;
                }
            }
            _logger.LogInformation("Delete scrape done.");
        }
    }
}
