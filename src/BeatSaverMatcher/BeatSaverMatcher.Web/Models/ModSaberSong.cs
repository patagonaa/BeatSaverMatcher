using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BeatSaverMatcher.Web.Models
{
    [JsonObject(MemberSerialization.Fields, NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ModSaberSong
    {
        public string Key { get; set; }
        public string Hash { get; set; }
        public string SongName { get; set; }
        public string Uploader { get; set; }
    }
}
