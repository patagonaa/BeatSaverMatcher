using BeatSaverMatcher.Common;
using BeatSaverMatcher.Common.Models;
using BeatSaverMatcher.Web.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Web
{
    public class MatchingService
    {
        private readonly SpotifyRepository _spotifyRepository;
        private readonly IBeatSaberSongRepository _songRepository;

        public MatchingService(SpotifyRepository spotifyRepository, IBeatSaberSongRepository songRepository)
        {
            _spotifyRepository = spotifyRepository;
            _songRepository = songRepository;
        }

        public async Task<IList<SongMatch>> GetMatches(string playlistId)
        {
            var tracks = await _spotifyRepository.GetTracksForPlaylist(playlistId);

            var matches = new List<SongMatch>();

            foreach (var track in tracks)
            {
                var match = new SongMatch
                {
                    SpotifyArtist = string.Join(", ", track.Artists.Select(x => x.Name)),
                    SpotifyTitle = track.Name
                };

                var beatmaps = new HashSet<BeatSaberSong>();

                if (track.Artists.Count == 1)
                {
                    var directMatches = await _songRepository.GetDirectMatches(track.Artists[0].Name, track.Name);
                    foreach (var beatmap in directMatches)
                    {
                        beatmaps.Add(beatmap);
                    }
                }

                if (beatmaps.Any())
                {
                    match.Matches = beatmaps.ToList();
                    matches.Add(match);
                }
            }

            return matches;
        }
    }
}
