using BeatSaverMatcher.Common.Db;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BeatSaverMatcher.Web.Models
{
    public class SongMatch
    {
        public int PlaylistIndex { get; init; }
        public string PlaylistArtist { get; init; }
        public string PlaylistTitle { get; init; }
        [JsonIgnore]
        public IList<BeatSaberSongWithRatings> DbBeatMaps { get; set; }
        public IList<BeatSaberSongViewModel> BeatMaps { get; set; }
    }
}
