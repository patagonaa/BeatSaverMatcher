using System;

namespace BeatSaverMatcher.Common.Db
{
    public class BeatSaberSong
    {
        required public string LevelAuthorName { get; init; }
        required public string SongAuthorName { get; init; }
        required public string SongName { get; init; }
        required public string SongSubName { get; init; }
        public double Bpm { get; init; }

        required public string Name { get; init; }

        public SongDifficulties Difficulties { get; init; }
        required public string Uploader { get; init; }
        public DateTime? Uploaded { get; init; }
        required public byte[] Hash { get; init; }
        public int BeatSaverKey { get; init; }
        public bool AutoMapper { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public DateTime? LastPublishedAt { get; init; }
        public DateTime? DeletedAt { get; init; }
    }
}
