using BeatSaverMatcher.Common.Db;
using System;

namespace BeatSaverMatcher.Web.Models
{
    public class BeatSaberSongViewModel
    {
        public string LevelAuthorName { get; init; }
        public string SongAuthorName { get; init; }
        public string SongName { get; init; }
        public string SongSubName { get; init; }
        public double Bpm { get; init; }

        public string Name { get; init; }

        public SongDifficulties Difficulties { get; init; }
        public string Uploader { get; init; }
        public DateTime Uploaded { get; init; }
        public byte[] Hash { get; init; }
        public int BeatSaverKey { get; init; }
        public double? Rating { get; init; }
        public int? UpVotes { get; init; }
        public int? DownVotes { get; init; }
    }
}
