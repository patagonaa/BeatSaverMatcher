using System;
using System.Collections.Generic;

namespace BeatSaverMatcher.Common.BeatSaver
{
    public class BeatSaverSong
    {
        required public string Id { get; init; }
        required public string Name { get; init; }
        public string? Description { get; init; }
        required public BeatSaverUploader Uploader { get; init; }
        required public BeatSaverMetadata Metadata { get; init; }
        required public BeatSaverStats Stats { get; init; }
        public DateTime? Uploaded { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public DateTime? LastPublishedAt { get; init; }
        public bool Automapper { get; init; }
        public bool Ranked { get; init; }
        required public IList<BeatSaverVersion> Versions { get; init; }
        //...
    }

    public class BeatSaverUploader
    {
        required public string Name { get; init; }
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
        required public string LevelAuthorName { get; init; }
        required public string SongAuthorName { get; init; }
        required public string SongName { get; init; }
        required public string SongSubName { get; init; }
        public double Bpm { get; init; }
    }

    public class BeatSaverVersion
    {
        required public string Hash { get; init; }
        required public string State { get; init; }
        public DateTime CreatedAt { get; init; }
        required public IList<BeatSaverDifficulty> Diffs { get; init; }
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
        required public string Characteristic { get; init; }
    }

    public enum BeatSaverDifficultyType
    {
        Easy,
        Normal,
        Hard,
        Expert,
        ExpertPlus
    }
}
