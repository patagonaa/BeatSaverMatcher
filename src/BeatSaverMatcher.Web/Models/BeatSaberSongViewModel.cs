﻿using BeatSaverMatcher.Common.Db;
using System;

namespace BeatSaverMatcher.Web.Models
{
    public class BeatSaberSongViewModel
    {
        public string LevelAuthorName { get; set; }
        public string SongAuthorName { get; set; }
        public string SongName { get; set; }
        public string SongSubName { get; set; }
        public double Bpm { get; set; }

        public string Name { get; set; }

        public SongDifficulties Difficulties { get; set; }
        public string Uploader { get; set; }
        public DateTime Uploaded { get; set; }
        public byte[] Hash { get; set; }
        public int BeatSaverKey { get; set; }
        public double? Rating { get; set; }
        public int? UpVotes { get; set; }
        public int? DownVotes { get; set; }
    }
}
