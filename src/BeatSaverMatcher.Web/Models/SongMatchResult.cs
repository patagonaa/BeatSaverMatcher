using System.Collections.Generic;

namespace BeatSaverMatcher.Web.Models
{
    public class SongMatchResult
    {
        public int MatchedSpotifySongs { get; set; }
        public int TotalSpotifySongs { get; set; }
        public IList<SongMatch> Matches { get; set; }
    }
}
