using System;

namespace BeatSaverMatcher.Common.Db
{
    public class BeatSaberSongRatings
    {
        public int BeatSaverKey { get; init; }
        public int Downvotes { get; init; }
        public int Upvotes { get; init; }
        public double Score { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
