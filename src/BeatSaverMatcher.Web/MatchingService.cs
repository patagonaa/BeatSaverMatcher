using BeatSaverMatcher.Common;
using BeatSaverMatcher.Common.BeatSaver;
using BeatSaverMatcher.Common.Models;
using BeatSaverMatcher.Web.Models;
using BeatSaverMatcher.Web.Result;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
                _logger.LogInformation("Loading Spotify Songs");
                item.State = SongMatchState.LoadingSpotifySongs;

                var tracks = await _spotifyRepository.GetTracksForPlaylist(item.PlaylistId);

                _logger.LogInformation("Finding Beatmaps");
                item.State = SongMatchState.SearchingBeatMaps;

                var matches = new List<SongMatch>();

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
                        var directMatches = await _songRepository.GetMatches(track.Artists[0].Name, track.Name);
                        foreach (var beatmap in directMatches)
                        {
                            beatmaps.Add(beatmap);
                        }
                    }
                    else
                    {
                        foreach (var artist in track.Artists)
                        {
                            var directMatches = await _songRepository.GetMatches(artist.Name, track.Name);
                            foreach (var beatmap in directMatches)
                            {
                                beatmaps.Add(beatmap);
                            }
                        }
                    }

                    if (beatmaps.Any())
                    {
                        match.BeatMaps = beatmaps.GroupBy(x => Convert.ToBase64String(x.Hash)).Select(x => x.First()).ToList();
                        matches.Add(match);
                    }
                }

                _logger.LogInformation("Loading Beatmap Ratings");
                item.State = SongMatchState.LoadingBeatMapRatings;

                foreach (var match in matches)
                {
                    foreach (var beatmap in match.BeatMaps)
                    {
                        var stats = await _statsService.GetStats(beatmap.BeatSaverKey);
                        beatmap.Rating = stats?.Rating;
                    }
                    match.BeatMaps = match.BeatMaps.OrderByDescending(x => x.Rating ?? 0).ToList();
                }

                item.Result = new SongMatchResult
                {
                    TotalSpotifySongs = tracks.Count,
                    MatchedSpotifySongs = matches.Count,
                    Matches = matches
                };
                _logger.LogInformation("Found {TrackCount} / {MatchCount} Songs!", tracks.Count, matches.Count);
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
