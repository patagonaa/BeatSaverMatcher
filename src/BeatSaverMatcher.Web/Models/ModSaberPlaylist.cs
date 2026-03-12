using System.Collections.Generic;

namespace BeatSaverMatcher.Web.Models
{
    public class ModSaberPlaylist
    {
        required public string PlaylistTitle { get; init; }
        required public string PlaylistAuthor { get; init; }
        public string? Image { get; init; }
        required public IList<ModSaberSong> Songs { get; init; }
    }
}
