using System;
using System.Collections.Generic;

namespace BeatSaverMatcher.Common.BeatSaver
{
#nullable enable
    public class BeatSaverSong
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public string? Description { get; init; }
        public BeatSaverUploader Uploader { get; init; }
        public BeatSaverMetadata Metadata { get; init; }
        public BeatSaverStats Stats { get; init; }
        public DateTime? Uploaded { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public DateTime? LastPublishedAt { get; init; }
        public bool Automapper { get; init; }
        public bool Ranked { get; init; }
        public IList<BeatSaverVersion> Versions { get; init; }
        //...
    }

    public class BeatSaverUploader
    {
        public string Name { get; init; }
        //...
    }

    public class BeatSaverStats
    {
        public int Downloads { get; init; }
        public int Plays { get; init; }
        public int Downvotes { get; init; }
        public int Upvotes { get; init; }
        public double Score { get; init; }
    }

    public class BeatSaverMetadata
    {
        public double Duration { get; init; }
        public string LevelAuthorName { get; init; }
        public string SongAuthorName { get; init; }
        public string SongName { get; init; }
        public string SongSubName { get; init; }
        public double Bpm { get; init; }
    }

    public class BeatSaverVersion
    {
        public string Hash { get; init; }
        public string State { get; init; }
        public DateTime CreatedAt { get; init; }
        public IList<BeatSaverDifficulty> Diffs { get; init; }
    }

    public class BeatSaverScore
    {
        //Hash
        public int MapId { get; init; }
        // Key64
        public int Upvotes { get; init; }
        public int Downvotes { get; init; }
        public double Score { get; init; }
    }

    public class BeatSaverDifficulty
    {
        public BeatSaverDifficultyType Difficulty { get; init; }
        public string Characteristic { get; init; }
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
