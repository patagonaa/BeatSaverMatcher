using BeatSaverMatcher.Api;
using BeatSaverMatcher.Api.Spotify;
using BeatSaverMatcher.Api.Tidal;
using BeatSaverMatcher.Common.Db;
using BeatSaverMatcher.Web.Models;
using BeatSaverMatcher.Web.Result;
using Microsoft.Extensions.Logging;
using Prometheus;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Web
{
    public class MatchingService
    {
        private static readonly Counter _playlistRequests = Metrics.CreateCounter("beatsaver_playlist_load_count", "number of times playlist was loaded", "provider", "result");
        private readonly SpotifyRepository _spotifyClient;
        private readonly TidalClient _tidalClient;
        private readonly IBeatSaberSongRepository _songRepository;
        private readonly ILogger<MatchingService> _logger;

        public MatchingService(SpotifyRepository spotifyClient, TidalClient tidalClient, IBeatSaberSongRepository songRepository, ILogger<MatchingService> logger)
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

                var idType = GetIdType(item.PlaylistId);
                if (idType == MusicServiceApiType.Unknown)
                {
                    _playlistRequests.WithLabels(idType.ToString(), "InvalidId").Inc();
                    throw new MatchingException("Invalid Playlist ID");
                }

                IList<PlaylistSong> tracks;
                try
                {
                    IMusicServiceApi client = GetClient(idType);

                    var progressCallback = (int current, int? total) => { item.ItemsProcessed = current; item.ItemsTotal = total; };
                    tracks = (await client.GetTracksForPlaylist(item.PlaylistId, progressCallback, cancellationToken))
                        .Where(x => x != null)
                        .ToList();

                    _playlistRequests.WithLabels(idType.ToString(), "OK").Inc();
                }
                catch (APIException aex)
                {
                    if (aex.StatusCode == HttpStatusCode.NotFound)
                    {
                        _playlistRequests.WithLabels(idType.ToString(), "NotFound").Inc();
                        throw new MatchingException("Error 404 while loading playlist: Not Found (is it public?)", aex);
                    }
                    else
                    {
                        _playlistRequests.WithLabels(idType.ToString(), "Error").Inc();
                        var sb = new StringBuilder();
                        sb.Append("Error ");
                        if (aex.StatusCode != null)
                            sb.Append($"{(int)aex.StatusCode.Value} ");
                        sb.Append("while loading playlist");
                        if (aex.Message != null)
                            sb.Append($": {aex.Message}");
                        throw new MatchingException(sb.ToString(), aex);
                    }
                }

                _logger.LogInformation("Finding beatmaps");
                item.State = SongMatchState.SearchingBeatMaps;

                var matches = new ConcurrentBag<SongMatch>();

                item.ItemsTotal = tracks.Count;
                item.ItemsProcessed = 0;

                var processed = 0;
                await Parallel.ForEachAsync(
                    tracks.Select((x, i) => (Track: x, Index: i)),
                    new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = 8 },
                    async (x, _) =>
                    {
                        var track = x.Track;

                        var dbBeatMaps = new List<BeatSaberSongWithRatings>();

                        foreach (var artist in track.Artists)
                        {
                            try
                            {
                                var directMatches = await _songRepository.GetMatches(artist, track.Name);
                                foreach (var beatmap in directMatches)
                                {
                                    dbBeatMaps.Add(beatmap);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Error while searching song in DB: {ArtistName} - {SongName}", artist, track.Name);
                                continue;
                            }
                        }

                        if (dbBeatMaps.Any())
                        {
                            var viewBeatMaps = new List<BeatSaberSongViewModel>();
                            foreach (var dbBeatMap in dbBeatMaps.DistinctBy(x => x.BeatSaverKey))
                            {
                                try
                                {
                                    viewBeatMaps.Add(MapBeatMap(dbBeatMap));
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error while mapping beatmap 0x{BeatMapKey}", dbBeatMap.BeatSaverKey.ToString("x"));
                                }
                            }

                            matches.Add(new SongMatch
                                {
                                    PlaylistIndex = x.Index,
                                    PlaylistArtist = string.Join(", ", track.Artists),
                                    PlaylistTitle = track.Name,
                                    BeatMaps = viewBeatMaps.OrderByDescending(x => x.Rating ?? 0).ToList(),
                                });
                        }
                        Interlocked.Increment(ref processed);

                        item.ItemsProcessed = processed;
                    });
                item.ItemsProcessed = processed;

                _logger.LogInformation("Found {MatchCount} / {TrackCount} songs!", matches.Count, tracks.Count);

                item.Result = new SongMatchResult
                {
                    TotalPlaylistSongs = tracks.Count,
                    MatchedPlaylistSongs = matches.Count,
                    Matches = matches.OrderBy(x => x.PlaylistIndex).ToList()
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

        private static BeatSaberSongViewModel MapBeatMap(BeatSaberSongWithRatings dbBeatMap)
        {
            return new BeatSaberSongViewModel
            {
                BeatSaverKey = dbBeatMap.BeatSaverKey,
                Hash = dbBeatMap.Hash,
                Uploader = dbBeatMap.Uploader,
                Uploaded = dbBeatMap.Uploaded ?? dbBeatMap.CreatedAt,
                Difficulties = dbBeatMap.Difficulties,
                Bpm = dbBeatMap.Bpm,
                LevelAuthorName = dbBeatMap.LevelAuthorName,
                SongAuthorName = dbBeatMap.SongAuthorName,
                SongName = dbBeatMap.SongName,
                SongSubName = dbBeatMap.SongSubName,
                Name = dbBeatMap.Name,
                Rating = dbBeatMap.Score,
                UpVotes = dbBeatMap.Upvotes,
                DownVotes = dbBeatMap.Downvotes
            };
        }

        private MusicServiceApiType GetIdType(string playlistId)
        {
            if (Guid.TryParse(playlistId, out _))
            {
                return MusicServiceApiType.Tidal;
            }
            if (Regex.IsMatch(playlistId, "[0-9A-Za-z]{22}"))
            {
                return MusicServiceApiType.Spotify;
            }
            return MusicServiceApiType.Unknown;
        }

        private IMusicServiceApi GetClient(MusicServiceApiType type)
        {
            return type switch
            {
                MusicServiceApiType.Spotify => _spotifyClient,
                MusicServiceApiType.Tidal => _tidalClient,
                _ => throw new ArgumentException("Invalid Music API Type")
            };
        }

        private class MatchingException : Exception
        {
            public MatchingException(string message, Exception? inner = null)
                : base(message, inner)
            {
            }
        }

        private enum MusicServiceApiType
        {
            Unknown = 0,
            Tidal,
            Spotify
        }
    }
}
