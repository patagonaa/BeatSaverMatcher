using BeatSaverMatcher.Common.BeatSaver;
using BeatSaverMatcher.Common.Db;
using BeatSaverMatcher.Web.Models;
using BeatSaverMatcher.Web.Result;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Web
{
    public class MatchingService
    {
        private readonly SpotifyRepository _spotifyRepository;
        private readonly IBeatSaberSongRepository _songRepository;
        private readonly ILogger<MatchingService> _logger;

        public MatchingService(SpotifyRepository spotifyRepository, IBeatSaberSongRepository songRepository, ILogger<MatchingService> logger)
        {
            _spotifyRepository = spotifyRepository;
            _songRepository = songRepository;
            _logger = logger;
        }

        public async Task GetMatches(WorkResultItem item, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Loading Spotify songs for playlist {PlaylistId}", item.PlaylistId);
                item.State = SongMatchState.LoadingSpotifySongs;
                item.ItemsTotal = 1;
                item.ItemsProcessed = 0;

                IList<FullTrack> tracks;

                try
                {
                    var progressCallback = (int current, int total) => { item.ItemsProcessed = current; item.ItemsTotal = total; };
                    tracks = (await _spotifyRepository.GetTracksForPlaylist(item.PlaylistId, progressCallback, cancellationToken))
                        .Where(x => x != null)
                        .ToList();
                }
                catch (APIException aex)
                {
                    throw new MatchingException($"Error while loading playlist: {aex.Message}");
                }

                _logger.LogInformation("Finding beatmaps");
                item.State = SongMatchState.SearchingBeatMaps;

                var matches = new List<SongMatch>();

                item.ItemsTotal = tracks.Count;
                item.ItemsProcessed = 0;

                foreach (var track in tracks)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var match = new SongMatch
                    {
                        SpotifyArtist = string.Join(", ", track.Artists.Select(x => x.Name)),
                        SpotifyTitle = track.Name
                    };

                    var beatmaps = new List<BeatSaberSongWithRatings>();

                    foreach (var artist in track.Artists)
                    {
                        try
                        {
                            var directMatches = await _songRepository.GetMatches(artist.Name, track.Name);
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

                    if (beatmaps.Any())
                    {
                        match.DbBeatMaps = beatmaps.GroupBy(x => x.BeatSaverKey).Select(x => x.First()).ToList();
                        matches.Add(match);
                    }

                    item.ItemsProcessed++;
                }

                _logger.LogInformation("Found {MatchCount} / {TrackCount} songs!", matches.Count, tracks.Count);

                foreach (var match in matches)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var foundBeatMaps = new List<BeatSaberSongViewModel>();
                    foreach (var dbBeatmap in match.DbBeatMaps)
                    {
                        try
                        {
                            foundBeatMaps.Add(new BeatSaberSongViewModel
                            {
                                BeatSaverKey = dbBeatmap.BeatSaverKey,
                                Hash = dbBeatmap.Hash,
                                Uploader = dbBeatmap.Uploader,
                                Uploaded = dbBeatmap.Uploaded,
                                Difficulties = dbBeatmap.Difficulties,
                                Bpm = dbBeatmap.Bpm,
                                LevelAuthorName = dbBeatmap.LevelAuthorName,
                                SongAuthorName = dbBeatmap.SongAuthorName,
                                SongName = dbBeatmap.SongName,
                                SongSubName = dbBeatmap.SongSubName,
                                Name = dbBeatmap.Name,
                                Rating = dbBeatmap.Score,
                                UpVotes = dbBeatmap.Upvotes,
                                DownVotes = dbBeatmap.Downvotes
                            });
                            item.ItemsProcessed++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error while mapping beatmap 0x{BeatMapKey}", dbBeatmap.BeatSaverKey.ToString("x"));
                        }
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
            catch (MatchingException ex)
            {
                item.State = SongMatchState.Error;
                item.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error while matching!");
            }
            catch (Exception ex)
            {
                item.State = SongMatchState.Error;
                _logger.LogError(ex, "Error while matching!");
            }
        }

        private class MatchingException : Exception
        {
            public MatchingException(string message)
                : base(message)
            {
            }
        }
    }
}
