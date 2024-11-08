﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Common.Db
{
    public interface IBeatSaberSongRepository
    {
        Task<int?> GetLatestBeatSaverKey();
        Task InsertSong(BeatSaberSong song);
        Task<IList<BeatSaberSong>> GetMatches(string artistName, string trackName, bool allowAutomapped);
        Task<IList<(bool AutoMapper, SongDifficulties Difficulties, int Count)>> GetSongCount();
        Task<bool> HasSong(int key);
        Task<int?> GetLatestBeatSaverKeyBefore(DateTime date);
    }
}