namespace BeatSaverMatcher.Common.Db
{
    public class BeatSaberSongWithRatings : BeatSaberSong
    {
        public int? Upvotes { get; set; }
        public int? Downvotes { get; set; }
        public double? Score { get; set; }
    }
}
