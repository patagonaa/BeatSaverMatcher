using System;

namespace BeatSaverMatcher.Common.Db
{
    public class BeatSaberSongWithRatings
    {
        public string LevelAuthorName { get; set; }
        public string SongAuthorName { get; set; }
        public string SongName { get; set; }
        public string SongSubName { get; set; }
        public double Bpm { get; set; }

        public string Name { get; set; }

        public SongDifficulties Difficulties { get; set; }
        public string Uploader { get; set; }
        public DateTime Uploaded { get; set; }
        public byte[] Hash { get; set; }
        public int BeatSaverKey { get; set; }
        public string AutoMapper { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastPublishedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? Upvotes { get; set; }
        public int? Downvotes { get; set; }
        public double? Score { get; set; }
    }
}
