
using System.Text.Json.Serialization;

namespace BeatSaverMatcher.Web.Models
{
    public class ModSaberSong
    {
        public string Key { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Hash { get; set; }
        public string SongName { get; set; }
        public string Uploader { get; set; }
    }
}
