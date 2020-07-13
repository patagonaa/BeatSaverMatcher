using BeatSaverMatcher.Common.Models;
using System.Collections.Generic;

namespace BeatSaverMatcher.Web.Models
{
    public class SongMatch
    {
        public string SpotifyArtist { get; set; }
        public string SpotifyTitle { get; set; }
        public IList<BeatSaberSong> BeatMaps { get; set; }
    }
}
