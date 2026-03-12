using System;

namespace BeatSaverMatcher.Common.Db
{
    public class BeatSaberSong
    {
        public string LevelAuthorName { get; init; }
        public string SongAuthorName { get; init; }
        public string SongName { get; init; }
        public string SongSubName { get; init; }
        public double Bpm { get; init; }

        public string Name { get; init; }

        public SongDifficulties Difficulties { get; init; }
        public string Uploader { get; init; }
        public DateTime? Uploaded { get; init; }
        public byte[] Hash { get; init; }
        public int BeatSaverKey { get; init; }
        public bool AutoMapper { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public DateTime? LastPublishedAt { get; init; }
        public DateTime? DeletedAt { get; init; }
    }
}
