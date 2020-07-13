using System;
using System.Linq;
using System.Threading.Tasks;
using BeatSaverMatcher.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace BeatSaverMatcher.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModSaberPlaylistController : ControllerBase
    {
        private readonly MatchingService _matchingService;
        private readonly SpotifyRepository _spotifyRepository;

        public ModSaberPlaylistController(MatchingService matchingService, SpotifyRepository spotifyRepository)
        {
            _matchingService = matchingService;
            _spotifyRepository = spotifyRepository;
        }

        [HttpGet("{playlistId}")]
        [HttpGet("{playlistId}/{keys}")]
        public async Task<ActionResult<ModSaberPlaylist>> GetMatchesAsPlaylistDownload([FromRoute] string playlistId, [FromRoute] string keys)
        {
            var playlist = await _spotifyRepository.GetPlaylist(playlistId);
            var matches = await _matchingService.GetMatches(playlistId);

            var beatmaps = matches.SelectMany(x => x.Matches).ToList();
            if (keys != null)
            {
                var keysList = keys.Split(',');
                beatmaps = beatmaps.Where(x => keysList.Contains(x.BeatSaverKey.ToString("x"))).ToList();
            }

            return new ModSaberPlaylist
            {
                PlaylistTitle = playlist.Name,
                PlaylistAuthor = (playlist.Owner?.DisplayName ?? "") + " using https://github.com/patagonaa/BeatSaverMatcher",
                Image = null, //TODO
                Songs = beatmaps.Select(x => new ModSaberSong
                {
                    Hash = string.Join("", x.Hash.Select(x => x.ToString("x2"))),
                    Key = x.BeatSaverKey.ToString("x"),
                    SongName = x.Name,
                    Uploader = x.Uploader
                }).ToList()
            };
        }
    }
}
