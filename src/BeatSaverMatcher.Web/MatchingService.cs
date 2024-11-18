﻿using BeatSaverMatcher.Common.BeatSaver;
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
        private readonly BeatSaverSongService _songService;
        private readonly ILogger<MatchingService> _logger;

        public MatchingService(SpotifyRepository spotifyRepository, IBeatSaberSongRepository songRepository, BeatSaverSongService songService, ILogger<MatchingService> logger)
        {
            _spotifyRepository = spotifyRepository;
            _songRepository = songRepository;
            _songService = songService;
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
                        match.DbBeatMaps = beatmaps.GroupBy(x => x.BeatSaverKey).Select(x => x.First()).ToList();
                        matches.Add(match);
                    }

                    item.ItemsProcessed++;
                }

                _logger.LogInformation("Found {MatchCount} / {TrackCount} songs!", matches.Count, tracks.Count);

                _logger.LogInformation("Loading beatmap ratings");
                item.State = SongMatchState.LoadingBeatMapRatings;

                item.ItemsTotal = matches.SelectMany(x => x.DbBeatMaps).Count();
                item.ItemsProcessed = 0;

                var beatSaverProgressCallback = (int current, int total) => { item.ItemsProcessed = current; item.ItemsTotal = total; };
                var beatSaverSongs = await _songService.GetSongs(matches.SelectMany(x => x.DbBeatMaps).Select(x => x.BeatSaverKey).Distinct().ToList(), beatSaverProgressCallback, cancellationToken);

                foreach (var match in matches)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var foundBeatMaps = new List<BeatSaberSongViewModel>();
                    foreach (var dbBeatmap in match.DbBeatMaps)
                    {
                        try
                        {
                            var beatSaverSong = beatSaverSongs.TryGetValue(dbBeatmap.BeatSaverKey, out var tmp) ? tmp : null;

                            if (beatSaverSong != null)
                            {
                                var currentVersion = beatSaverSong.Versions
                                    .Where(x => x.State == "Published")
                                    .OrderByDescending(x => x.CreatedAt)
                                    .FirstOrDefault();

                                if (currentVersion != null)
                                {
                                    foundBeatMaps.Add(new BeatSaberSongViewModel
                                    {
                                        BeatSaverKey = dbBeatmap.BeatSaverKey,
                                        Hash = BeatSaverUtils.MapHash(currentVersion.Hash),
                                        Uploader = beatSaverSong.Uploader.Name,
                                        Uploaded = beatSaverSong.Uploaded,
                                        Difficulties = BeatSaverUtils.MapDifficulties(currentVersion.Diffs),
                                        Bpm = beatSaverSong.Metadata.Bpm,
                                        LevelAuthorName = beatSaverSong.Metadata.LevelAuthorName,
                                        SongAuthorName = beatSaverSong.Metadata.SongAuthorName,
                                        SongName = beatSaverSong.Metadata.SongName,
                                        SongSubName = beatSaverSong.Metadata.SongSubName,
                                        Name = beatSaverSong.Name,
                                        Rating = beatSaverSong.Stats.Score,
                                        UpVotes = beatSaverSong.Stats.Upvotes,
                                        DownVotes = beatSaverSong.Stats.Downvotes
                                    });
                                }
                            }
                            // don't add beatmap if it wasn't found online (was deleted)
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
