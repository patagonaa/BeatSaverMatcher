using System;

namespace BeatSaverMatcher.Common.Models
{
    [Flags]
    public enum SongDifficulties
    {
        Easy = 1 << 0,
        Normal = 1 << 1,
        Hard = 1 << 2,
        Expert = 1 << 3,
        ExpertPlus = 1 << 4
    }
}
