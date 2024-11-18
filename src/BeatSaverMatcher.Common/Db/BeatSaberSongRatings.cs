using System;

namespace BeatSaverMatcher.Common.Db
{
    public class BeatSaberSongRatings
    {
        public int BeatSaverKey { get; set; }
        public int Downvotes { get; set; }
        public int Upvotes { get; set; }
        public double Score { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
