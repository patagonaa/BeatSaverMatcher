using BeatSaverMatcher.Common.Db;
using System;

namespace BeatSaverMatcher.Web.Models
{
    public class BeatSaberSongViewModel
    {
        required public string LevelAuthorName { get; init; }
        required public string SongAuthorName { get; init; }
        required public string SongName { get; init; }
        required public string SongSubName { get; init; }
        public double Bpm { get; init; }

        required public string Name { get; init; }

        public SongDifficulties Difficulties { get; init; }
        required public string Uploader { get; init; }
        public DateTime Uploaded { get; init; }
        required public byte[] Hash { get; init; }
        public int BeatSaverKey { get; init; }
        public double? Rating { get; init; }
        public int? UpVotes { get; init; }
        public int? DownVotes { get; init; }
    }
}
