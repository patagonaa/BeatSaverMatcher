using System.Collections.Generic;

namespace BeatSaverMatcher.Common.BeatSaver
{
    internal class BeatSaverSongSearchResponse
    {
        required public IList<BeatSaverSong> Docs { get; init; }
    }
}
