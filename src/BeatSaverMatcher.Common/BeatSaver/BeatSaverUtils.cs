using BeatSaverMatcher.Common.Db;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BeatSaverMatcher.Common.BeatSaver
{
    public static class BeatSaverUtils
    {
        public static byte[] MapHash(string hash)
        {
            byte[] toReturn = new byte[hash.Length / 2];
            for (int i = 0; i < hash.Length; i += 2)
                toReturn[i / 2] = Convert.ToByte(hash.Substring(i, 2), 16);
            return toReturn;
        }

        public static SongDifficulties MapDifficulties(IList<BeatSaverDifficulty> difficulties)
        {
            SongDifficulties toReturn = 0;
            if (difficulties.Any(x => x.Difficulty == BeatSaverDifficultyType.Easy))
                toReturn |= SongDifficulties.Easy;
            if (difficulties.Any(x => x.Difficulty == BeatSaverDifficultyType.Normal))
                toReturn |= SongDifficulties.Normal;
            if (difficulties.Any(x => x.Difficulty == BeatSaverDifficultyType.Hard))
                toReturn |= SongDifficulties.Hard;
            if (difficulties.Any(x => x.Difficulty == BeatSaverDifficultyType.Expert))
                toReturn |= SongDifficulties.Expert;
            if (difficulties.Any(x => x.Difficulty == BeatSaverDifficultyType.ExpertPlus))
                toReturn |= SongDifficulties.ExpertPlus;
            return toReturn;
        }
    }
}
