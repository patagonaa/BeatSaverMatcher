using BeatSaverMatcher.Web.Models;
using System;

namespace BeatSaverMatcher.Web.Result
{
    public class WorkResultItem
    {
        public WorkResultItem(string playlistId)
        {
            PlaylistId = playlistId;
            State = SongMatchState.Waiting;
            CreatedAt = DateTime.UtcNow;
        }

        public string PlaylistId { get; }
        public SongMatchState State { get; set; }
        public int ItemsProcessed { get; set; }
        public int ItemsTotal { get; set; }
        public SongMatchResult Result { get; set; }
        public DateTime CreatedAt { get; }
        public bool IsFinished => State == SongMatchState.Finished || State == SongMatchState.Error;
    }
}
