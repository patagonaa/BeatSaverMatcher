using System.Collections.Generic;

namespace BeatSaverMatcher.Common.BeatSaver
{
    internal class BeatSaverSongDeletedResponse
    {
        required public IList<BeatSaverDeletedSong> Docs { get; init; }
    }
}
