using BeatSaverMatcher.Common.Db;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BeatSaverMatcher.Web.Models
{
    public class SongMatch
    {
        public int PlaylistIndex { get; init; }
        required public string PlaylistArtist { get; init; }
        required public string PlaylistTitle { get; init; }
        [JsonIgnore]
        required public IList<BeatSaberSongWithRatings> DbBeatMaps { get; init; }
        public IList<BeatSaberSongViewModel>? BeatMaps { get; set; }
    }
}
