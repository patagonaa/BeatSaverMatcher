using BeatSaverMatcher.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Common
{
    public interface IBeatSaberSongRepository
    {
        Task<int?> GetLatestBeatSaverKey();
        Task InsertSong(BeatSaberSong song);
        Task<IList<BeatSaberSong>> GetMatches(string artistName, string trackName, bool allowAutomapped);
    }
}