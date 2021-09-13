using BeatSaverMatcher.Common;
using BeatSaverMatcher.Common.BeatSaver;
using BeatSaverMatcher.Common.Models;
using BeatSaverMatcher.Web.Models;
using BeatSaverMatcher.Web.Result;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Web
{
    public class MatchingService
    {
        private readonly SpotifyRepository _spotifyRepository;
        private readonly IBeatSaberSongRepository _songRepository;
        private readonly BeatSaverStatsService _statsService;
        private readonly ILogger<MatchingService> _logger;

        public MatchingService(SpotifyRepository spotifyRepository, IBeatSaberSongRepository songRepository, BeatSaverStatsService statsService, ILogger<MatchingService> logger)
        {
            _spotifyRepository = spotifyRepository;
            _songRepository = songRepository;
            _statsService = statsService;
            _logger = logger;
        }

        public async Task GetMatches(WorkResultItem item)
        {
            try
            {
                _logger.LogInformation("Loading Spotify Songs for Playlist {PlaylistId}", item.PlaylistId);
                item.State = SongMatchState.LoadingSpotifySongs;
                item.ItemsTotal = 1;
                item.ItemsProcessed = 0;

                var tracks = await _spotifyRepository.GetTracksForPlaylist(item.PlaylistId);

                _logger.LogInformation("Finding Beatmaps");
                item.State = SongMatchState.SearchingBeatMaps;

                var matches = new List<SongMatch>();

                item.ItemsTotal = tracks.Count;
                item.ItemsProcessed = 0;

                foreach (var track in tracks)
                {
                    var match = new SongMatch
                    {
                        SpotifyArtist = string.Join(", ", track.Artists.Select(x => x.Name)),
                        SpotifyTitle = track.Name
                    };

                    var beatmaps = new List<BeatSaberSong>();

                    if (track.Artists.Count == 1)
                    {
                        try
                        {
                            var directMatches = await _songRepository.GetMatches(track.Artists[0].Name, track.Name, false);
                            foreach (var beatmap in directMatches)
                            {
                                beatmaps.Add(beatmap);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error while searching song in DB: {ArtistName} - {SongName}", track.Artists[0].Name, track.Name);
                            continue;
                        }
                    }
                    else
                    {
                        foreach (var artist in track.Artists)
                        {
                            try
                            {
                                var directMatches = await _songRepository.GetMatches(artist.Name, track.Name, false);
                                foreach (var beatmap in directMatches)
                                {
                                    beatmaps.Add(beatmap);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Error while searching song in DB: {ArtistName} - {SongName}", artist.Name, track.Name);
                                continue;
                            }
                        }
                    }

                    if (beatmaps.Any())
                    {
                        match.BeatMaps = beatmaps.GroupBy(x => Convert.ToBase64String(x.Hash)).Select(x => x.First()).ToList();
                        matches.Add(match);
                    }

                    item.ItemsProcessed++;
                }

                _logger.LogInformation("Found {MatchCount} / {TrackCount} Songs!", matches.Count, tracks.Count);

                _logger.LogInformation("Loading Beatmap Ratings");
                item.State = SongMatchState.LoadingBeatMapRatings;

                item.ItemsTotal = matches.SelectMany(x => x.BeatMaps).Count();
                item.ItemsProcessed = 0;

                foreach (var match in matches)
                {
                    var foundBeatMaps = new List<BeatSaberSong>();
                    foreach (var beatmap in match.BeatMaps)
                    {
                        try
                        {
                            var stats = await _statsService.GetStats(beatmap.BeatSaverKey);
                            if (stats != null)
                            {
                                beatmap.Rating = stats.Score;
                                beatmap.UpVotes = stats.Upvotes;
                                beatmap.DownVotes = stats.Downvotes;
                                foundBeatMaps.Add(beatmap);
                            }
                            // don't add beatmap if it wasn't found online (was deleted)
                        }
                        catch (TimeoutException)
                        {
                            foundBeatMaps.Add(beatmap);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error while loading Beatmap Stats for 0x{BeatMapKey}", beatmap.BeatSaverKey.ToString("x"));
                        }

                        item.ItemsProcessed++;
                    }
                    match.BeatMaps = foundBeatMaps.OrderByDescending(x => x.Rating ?? 0).ToList();
                }

                item.Result = new SongMatchResult
                {
                    TotalSpotifySongs = tracks.Count,
                    MatchedSpotifySongs = matches.Count,
                    Matches = matches
                };
                _logger.LogInformation("Done.");
                item.State = SongMatchState.Finished;
            }
            catch (Exception ex)
            {
                item.State = SongMatchState.Error;
                _logger.LogError(ex, "Error while Matching!");
            }
        }
    }
}
