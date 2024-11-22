﻿namespace BeatSaverMatcher.Web.Result
{
    public enum SongMatchState
    {
        None,
        Waiting,
        LoadingSpotifySongs,
        SearchingBeatMaps,
        Finished = 5,
        Error
    }
}