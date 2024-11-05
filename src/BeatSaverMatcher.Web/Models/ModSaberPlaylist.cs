using System.Collections.Generic;

namespace BeatSaverMatcher.Web.Models
{
    public class ModSaberPlaylist
    {
        public string PlaylistTitle { get; set; }
        public string PlaylistAuthor { get; set; }
        public string Image { get; set; }
        public IList<ModSaberSong> Songs { get; set; }
    }
}
