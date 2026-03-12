using System.Collections.Generic;

namespace BeatSaverMatcher.Web.Models
{
    public class SongMatchResult
    {
        public int MatchedPlaylistSongs { get; init; }
        public int TotalPlaylistSongs { get; init; }
        required public IList<SongMatch> Matches { get; init; }
    }
}
