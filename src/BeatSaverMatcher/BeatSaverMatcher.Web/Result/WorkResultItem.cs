using BeatSaverMatcher.Web.Models;

namespace BeatSaverMatcher.Web.Result
{
    public class WorkResultItem
    {
        public WorkResultItem(string playlistId)
        {
            PlaylistId = playlistId;
            State = SongMatchState.Waiting;
        }

        public string PlaylistId { get; }
        public SongMatchState State { get; set; }
        public SongMatchResult Result { get; set; }
    }
}
