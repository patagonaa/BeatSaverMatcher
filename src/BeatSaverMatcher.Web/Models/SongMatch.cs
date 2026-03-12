using System.Collections.Generic;

namespace BeatSaverMatcher.Web.Models
{
    public class SongMatch
    {
        public int PlaylistIndex { get; init; }
        required public string PlaylistArtist { get; init; }
        required public string PlaylistTitle { get; init; }
        public IList<BeatSaberSongViewModel>? BeatMaps { get; set; }
    }
}
