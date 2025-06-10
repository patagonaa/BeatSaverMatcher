using System.Collections.Generic;

namespace BeatSaverMatcher.Web.Models
{
    public class SongMatchResult
    {
        public int MatchedPlaylistSongs { get; set; }
        public int TotalPlaylistSongs { get; set; }
        public IList<SongMatch> Matches { get; set; }
    }
}
