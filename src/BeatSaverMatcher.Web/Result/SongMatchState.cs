namespace BeatSaverMatcher.Web.Result
{
    public enum SongMatchState
    {
        None,
        Waiting,
        LoadingSpotifySongs,
        SearchingBeatMaps,
        LoadingBeatMapRatings,
        Finished,
        Error
    }
}