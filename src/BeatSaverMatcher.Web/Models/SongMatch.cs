using BeatSaverMatcher.Common.Db;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BeatSaverMatcher.Web.Models
{
    public class SongMatch
    {
        public string PlaylistArtist { get; set; }
        public string PlaylistTitle { get; set; }
        [JsonIgnore]
        public IList<BeatSaberSongWithRatings> DbBeatMaps { get; set; }
        public IList<BeatSaberSongViewModel> BeatMaps { get; set; }
    }
}
