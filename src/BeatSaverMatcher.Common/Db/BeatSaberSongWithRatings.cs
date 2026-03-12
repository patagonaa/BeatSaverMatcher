namespace BeatSaverMatcher.Common.Db
{
    public class BeatSaberSongWithRatings : BeatSaberSong
    {
        public int? Upvotes { get; init; }
        public int? Downvotes { get; init; }
        public double? Score { get; init; }
    }
}
