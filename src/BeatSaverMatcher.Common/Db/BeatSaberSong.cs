using System.Collections.Generic;

namespace BeatSaverMatcher.Common.Models
{
    public class BeatSaberSong
    {
        public string LevelAuthorName { get; set; }
        public string SongAuthorName { get; set; }
        public string SongName { get; set; }
        public string SongSubName { get; set; }
        public double Bpm { get; set; }

        public string Name { get; set; }

        public SongDifficulties Difficulties { get; set; }
        public string Uploader { get; set; }
        public byte[] Hash { get; set; }
        public int BeatSaverKey { get; set; }
        public double? Rating { get; set; }

        public override bool Equals(object obj)
        {
            return obj is BeatSaberSong song &&
                   EqualityComparer<byte[]>.Default.Equals(Hash, song.Hash);
        }

        public override int GetHashCode()
        {
            return -1545866855 + EqualityComparer<byte[]>.Default.GetHashCode(Hash);
        }
    }
}
