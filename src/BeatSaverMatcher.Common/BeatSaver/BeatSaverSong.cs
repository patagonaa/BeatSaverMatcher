using System;
using System.Collections.Generic;

namespace BeatSaverMatcher.Common.BeatSaver
{
#nullable enable
    public class BeatSaverSong
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public BeatSaverUploader Uploader { get; set; }
        public BeatSaverMetadata Metadata { get; set; }
        public BeatSaverStats Stats { get; set; }
        public DateTime Uploaded { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime LastPublishedAt { get; set; }
        public bool Automapper { get; set; }
        public bool Ranked { get; set; }
        public IList<BeatSaverVersion> Versions { get; set; }
        //...
    }

    public class BeatSaverUploader
    {
        public string Name { get; set; }
        //...
    }

    public class BeatSaverStats
    {
        public int Downloads { get; set; }
        public int Plays { get; set; }
        public int Downvotes { get; set; }
        public int Upvotes { get; set; }
        public double Score { get; set; }
    }

    public class BeatSaverMetadata
    {
        public double Duration { get; set; }
        public string LevelAuthorName { get; set; }
        public string SongAuthorName { get; set; }
        public string SongName { get; set; }
        public string SongSubName { get; set; }
        public double Bpm { get; set; }
    }

    public class BeatSaverVersion
    {
        public string Hash { get; set; }
        public string State { get; set; }
        public DateTime CreatedAt { get; set; }
        public IList<BeatSaverDifficulty> Diffs { get; set; }
    }

    public class BeatSaverScore
    {
        //Hash
        public int MapId { get; set; }
        // Key64
        public int Upvotes { get; set; }
        public int Downvotes { get; set; }
        public double Score { get; set; }
    }

    public class BeatSaverDifficulty
    {
        public BeatSaverDifficultyType Difficulty { get; set; }
        public string Characteristic { get; set; }
    }

    public enum BeatSaverDifficultyType
    {
        Easy,
        Normal,
        Hard,
        Expert,
        ExpertPlus
    }
#nullable restore
}
