namespace BeatSaverMatcher.Web.Models
{
    public class ModSaberSong
    {
        required public string Key { get; init; }
        required public string Hash { get; init; }
        required public string SongName { get; init; }
        required public string Uploader { get; init; }
    }
}
