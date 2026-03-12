using System;

namespace BeatSaverMatcher.Common.BeatSaver
{
    public class BeatSaverDeletedSong
    {
        required public string Id { get; init; }
        public DateTime DeletedAt { get; init; }
    }
}
