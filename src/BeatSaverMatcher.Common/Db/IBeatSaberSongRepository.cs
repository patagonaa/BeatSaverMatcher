using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Common.Db
{
    public interface IBeatSaberSongRepository
    {
        Task<int?> GetLatestBeatSaverKey();
        Task<IList<int>> GetAllKeys(CancellationToken cancellationToken);
        Task<bool> InsertOrUpdateSong(BeatSaberSong song);
        Task<IList<BeatSaberSongWithRatings>> GetMatches(string artistName, string trackName);
        Task<IList<(bool AutoMapper, SongDifficulties Difficulties, int Count)>> GetSongCount();
        Task<DateTime?> GetLatestUpdatedAt(CancellationToken token);
        Task<DateTime?> GetLatestScoreUpdatedAt(CancellationToken token);
        Task MarkDeleted(int key, DateTime deletedAt);
        Task<bool> InsertOrUpdateSongRatings(BeatSaberSongRatings ratings);
    }
}