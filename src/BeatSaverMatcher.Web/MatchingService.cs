using BeatSaverMatcher.Api;
using BeatSaverMatcher.Api.Spotify;
using BeatSaverMatcher.Api.Tidal;
using BeatSaverMatcher.Common.Db;
using BeatSaverMatcher.Web.Models;
using BeatSaverMatcher.Web.Result;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Web
{
    public class MatchingService
    {
        private readonly SpotifyRepository _spotifyClient;
        private readonly TidalClient _tidalClient;
        private readonly IBeatSaberSongRepository _songRepository;
        private readonly ILogger<MatchingService> _logger;

        public MatchingService(SpotifyRepository spotifyClient, TidalClient tidalClient, IBeatSaberSongRepository songRepository, ILogger<MatchingService> logger, ILogger<TidalClient> logger1)
        {
            _spotifyClient = spotifyClient;
            _tidalClient = tidalClient;
            _songRepository = songRepository;
            _logger = logger;
        }

        public async Task GetMatches(WorkResultItem item, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Loading songs for playlist {PlaylistId}", item.PlaylistId);
                item.State = SongMatchState.LoadingPlaylistSongs;
                item.ItemsTotal = 1;
                item.ItemsProcessed = 0;

                IList<PlaylistSong> tracks;

                IMusicServiceApi client = Guid.TryParse(item.PlaylistId, out _) ? _tidalClient : _spotifyClient;

                try
                {
                    var progressCallback = (int current, int total) => { item.ItemsProcessed = current; item.ItemsTotal = total; };
                    tracks = (await client.GetTracksForPlaylist(item.PlaylistId, progressCallback, cancellationToken))
                        .Where(x => x != null)
                        .ToList();
                }
                catch (APIException aex)
                {
                    throw new MatchingException($"Error while loading playlist: {aex.Message}", aex);
                }

                _logger.LogInformation("Finding beatmaps");
                item.State = SongMatchState.SearchingBeatMaps;

                var matches = new ConcurrentBag<SongMatch>();

                item.ItemsTotal = tracks.Count;
                item.ItemsProcessed = 0;

                var processed = 0;
                await Parallel.ForEachAsync(
                    tracks,
                    new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = 8 },
                    async (track, _) =>
                    {
                        var match = new SongMatch
                        {
                            PlaylistArtist = string.Join(", ", track.Artists),
                            PlaylistTitle = track.Name
                        };

                        var beatmaps = new List<BeatSaberSongWithRatings>();

                        foreach (var artist in track.Artists)
                        {
                            try
                            {
                                var directMatches = await _songRepository.GetMatches(artist, track.Name);
                                foreach (var beatmap in directMatches)
                                {
                                    beatmaps.Add(beatmap);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Error while searching song in DB: {ArtistName} - {SongName}", artist, track.Name);
                                continue;
                            }
                        }

                        if (beatmaps.Any())
                        {
                            match.DbBeatMaps = beatmaps.GroupBy(x => x.BeatSaverKey).Select(x => x.First()).ToList();
                            matches.Add(match);
                        }
                        Interlocked.Increment(ref processed);

                        item.ItemsProcessed = processed;
                    });
                item.ItemsProcessed = processed;

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
                    TotalPlaylistSongs = tracks.Count,
                    MatchedPlaylistSongs = matches.Count,
                    Matches = matches.ToList()
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
            public MatchingException(string message, Exception inner = null)
                : base(message, inner)
            {
            }
        }
    }
}
