using System.Collections.Generic;

namespace BeatSaverMatcher.Common.BeatSaver
{
    internal class BeatSaverSongDeletedResponse
    {
        public IList<BeatSaverDeletedSong> Docs { get; set; }
    }
}
