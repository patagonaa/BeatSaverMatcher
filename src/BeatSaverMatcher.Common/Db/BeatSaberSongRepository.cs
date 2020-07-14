using BeatSaverMatcher.Common.Models;
using Dapper;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Common
{
    public class BeatSaberSongRepository : SqlServerRepository, IBeatSaberSongRepository
    {
        public BeatSaberSongRepository(IOptions<DbConfiguration> options)
            : base(options)
        {
        }

        public async Task<IList<BeatSaberSong>> GetMatches(string artistName, string trackName)
        {
            using (var connection = GetConnection())
            {
                var results = await connection.QueryAsync<BeatSaberSong>("SELECT * FROM [dbo].[BeatSaberSong] WHERE (TextSearchValue LIKE '%' + @ArtistName + '%' AND TextSearchValue LIKE '%' + @TrackName + '%')", new { ArtistName = artistName, TrackName = trackName });
                return results.ToList();
            }
        }

        public async Task<int?> GetLatestBeatSaverKey()
        {
            using (var connection = GetConnection())
            {
                var sqlStr = @"SELECT TOP 1 BeatSaverKey FROM [dbo].[BeatSaberSong] ORDER BY BeatSaverKey DESC";

                using (var command = new SqlCommand(sqlStr, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                        {
                            return null;
                        }
                        return reader.GetInt32(0);
                    }
                }
            }
        }

        public async Task InsertSong(BeatSaberSong song)
        {
            using (var connection = GetConnection())
            {
                var sqlStr = @"
    INSERT INTO [dbo].[BeatSaberSong]
        ([Hash],[BeatSaverKey],[Uploader],[Difficulties],[LevelAuthorName],[SongAuthorName],[SongName],[SongSubName],[Bpm],[Name],[TextSearchValue])
        VALUES (@hash, @key, @uploader, @difficulties, @levelAuthorName, @songAuthorName, @songName, @songSubName, @bpm, @name, @textSearchValue)";

                using (var command = new SqlCommand(sqlStr, connection))
                {
                    command.Parameters.AddWithValue("hash", song.Hash);
                    command.Parameters.AddWithValue("key", song.BeatSaverKey);
                    command.Parameters.AddWithValue("uploader", song.Uploader);
                    command.Parameters.AddWithValue("difficulties", (int)song.Difficulties);
                    command.Parameters.AddWithValue("levelAuthorName", song.LevelAuthorName);
                    command.Parameters.AddWithValue("songAuthorName", song.SongAuthorName);
                    command.Parameters.AddWithValue("songName", song.SongName);
                    command.Parameters.AddWithValue("songSubName", song.SongSubName);
                    command.Parameters.AddWithValue("bpm", song.Bpm);
                    command.Parameters.AddWithValue("name", song.Name);
                    command.Parameters.AddWithValue("textSearchValue", string.Join("|", song.LevelAuthorName, song.SongAuthorName, song.SongName, song.SongSubName, song.Name));

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
