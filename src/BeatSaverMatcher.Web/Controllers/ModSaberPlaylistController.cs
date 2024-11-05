﻿using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using BeatSaverMatcher.Web.Models;
using BeatSaverMatcher.Web.Result;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;

namespace BeatSaverMatcher.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModSaberPlaylistController : ControllerBase
    {
        private readonly WorkItemStore _itemStore;
        private readonly SpotifyRepository _spotifyRepository;
        private readonly ILogger<ModSaberPlaylistController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public ModSaberPlaylistController(WorkItemStore itemStore, SpotifyRepository spotifyRepository, ILogger<ModSaberPlaylistController> logger, IHttpClientFactory httpClientFactory)
        {
            _itemStore = itemStore;
            _spotifyRepository = spotifyRepository;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("{playlistId}.bplist")]
        [HttpGet("{keys}/{playlistId}.bplist")]
        public async Task<ActionResult<ModSaberPlaylist>> GetMatchesAsPlaylistDownload([FromRoute] string playlistId, [FromRoute] string keys, CancellationToken cancellationToken)
        {
            var workItem = _itemStore.Get(playlistId);
            if (workItem == null)
                NotFound();

            if (workItem.State != SongMatchState.Finished)
                BadRequest();

            var playlist = await _spotifyRepository.GetPlaylist(playlistId, cancellationToken);

            var beatmaps = workItem.Result.Matches.SelectMany(x => x.BeatMaps).ToList();
            if (keys != null)
            {
                var keysList = keys.Split(',');
                beatmaps = beatmaps.Where(x => keysList.Contains(x.BeatSaverKey.ToString("x"))).ToList();
            }
            var header = new ContentDispositionHeaderValue("attachment") { FileName = playlist.Id + ".bplist", FileNameStar = playlist.Name + ".bplist" };
            Response.Headers.Add("Content-Disposition", header.ToString());

            return new ModSaberPlaylist
            {
                PlaylistTitle = playlist.Name,
                PlaylistAuthor = (playlist.Owner?.DisplayName ?? "") + " using https://github.com/patagonaa/BeatSaverMatcher",
                Image = await GetImage(playlist, cancellationToken),
                Songs = beatmaps.Select(x => new ModSaberSong
                {
                    // don't set hash as it might change when the map is updated.
                    // at least ModAssistant can deal with there not being a hash (and fetches by key instead)
                    //Hash = string.Join("", x.Hash.Select(x => x.ToString("x2"))),
                    Key = x.BeatSaverKey.ToString("x"),
                    SongName = x.Name,
                    Uploader = x.Uploader
                }).ToList()
            };
        }

        private async Task<string> GetImage(FullPlaylist playlist, CancellationToken cancellationToken)
        {
            try
            {
                var imageUrl = playlist.Images?.LastOrDefault(x => Math.Max(x.Width, x.Height) >= 256)?.Url;
                if (imageUrl == null)
                {
                    return null;
                }

                var imageBytes = await _httpClientFactory.CreateClient().GetByteArrayAsync(imageUrl, cancellationToken);

                return Convert.ToBase64String(imageBytes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Couldn't get image for playlist");
                return null;
            }
        }
    }
}
