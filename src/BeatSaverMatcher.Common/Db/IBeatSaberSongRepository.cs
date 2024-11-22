using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Common.Db
{
    public interface IBeatSaberSongRepository
    {
        Task<IList<BeatSaberSongWithRatings>> GetMatches(string artistName, string trackName);
        Task<int?> GetLatestBeatSaverKey();
        Task<IList<(bool AutoMapper, SongDifficulties Difficulties, int Count)>> GetSongCount();
        Task<DateTime?> GetLatestUpdatedAt(CancellationToken token);
        Task<DateTime?> GetLatestScoreUpdatedAt(CancellationToken token);
        Task<DateTime?> GetLatestDeletedAt(CancellationToken token);
        Task<bool> InsertOrUpdateSong(BeatSaberSong song);
        Task<bool> InsertOrUpdateSongRatings(BeatSaberSongRatings ratings);
        Task MarkDeleted(int key, DateTime deletedAt);
    }
}