using System;
using System.Collections.Generic;

namespace BeatSaverMatcher.Common.BeatSaver
{
#nullable enable
    public class BeatSaverSong
    {
        public BeatSaverMetadata Metadata { get; set; }
        public BeatSaverStats Stats { get; set; }
        public string? Description { get; set; }
        public string? DeletedAt { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public BeatSaverUploader Uploader { get; set; }
        public string Hash { get; set; }
        public DateTime Uploaded { get; set; }
        public string Automapper { get; set; }
        //...
    }

    public class BeatSaverUploader
    {
        public string Username { get; set; }
        //...
    }

    public class BeatSaverStats
    {
        public int Downloads { get; set; }
        public int Plays { get; set; }
        public int DownVotes { get; set; }
        public int UpVotes { get; set; }
        public double Heat { get; set; }
        public double Rating { get; set; }
    }

    public class BeatSaverMetadata
    {
        public BeatSaverDifficulties Difficulties { get; set; }
        public double Duration { get; set; }
        public string? Automapper { get; set; }
        public IList<BeatSaverCharacteristic> Characteristics { get; set; }
        public string LevelAuthorName { get; set; }
        public string SongAuthorName { get; set; }
        public string SongName { get; set; }
        public string SongSubName { get; set; }
        public double Bpm { get; set; }
    }

    public class BeatSaverCharacteristic
    {
        public string Name { get; set; }
        public BeatSaverCharacteristicDifficulties Difficulties { get; set; }
    }

    public class BeatSaverCharacteristicDifficulties
    {
        public BeatSaverCharacteristicDifficulty? Easy { get; set; }
        public BeatSaverCharacteristicDifficulty? Normal { get; set; }
        public BeatSaverCharacteristicDifficulty? Hard { get; set; }
        public BeatSaverCharacteristicDifficulty? Expert { get; set; }
        public BeatSaverCharacteristicDifficulty? ExpertPlus { get; set; }
    }

    public class BeatSaverCharacteristicDifficulty
    {
        public double Duration { get; set; }
        public int Length { get; set; }
        //...
    }

    public class BeatSaverDifficulties
    {
        public bool Easy { get; set; }
        public bool Normal { get; set; }
        public bool Hard { get; set; }
        public bool Expert { get; set; }
        public bool ExpertPlus { get; set; }
    }
#nullable restore
}
