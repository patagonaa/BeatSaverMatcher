namespace BeatSaverMatcher.Web.Result
{
    public enum SongMatchState
    {
        None,
        Waiting,
        LoadingPlaylistSongs,
        SearchingBeatMaps,
        Finished = 5,
        Error
    }
}