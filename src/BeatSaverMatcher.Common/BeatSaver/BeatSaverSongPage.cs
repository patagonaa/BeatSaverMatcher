using System.Collections.Generic;

namespace BeatSaverMatcher.Common.BeatSaver
{
    class BeatSaverSongPage
    {
        public IList<BeatSaverSong> Docs { get; set; }
        public int TotalDocs { get; set; }
        public int LastPage { get; set; }
        public int? PrevPage { get; set; }
        public int? NextPage { get; set; }
    }
}
