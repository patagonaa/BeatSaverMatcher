using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Common.Db
{
    public interface IBeatSaberSongRepository
    {
        Task<int?> GetLatestBeatSaverKey();
        Task<bool> InsertOrUpdateSong(BeatSaberSong song);
        Task<IList<BeatSaberSong>> GetMatches(string artistName, string trackName, bool allowAutomapped);
        Task<IList<(bool AutoMapper, SongDifficulties Difficulties, int Count)>> GetSongCount();
        Task<DateTime?> GetLatestUpdatedAt(System.Threading.CancellationToken token);
    }
}