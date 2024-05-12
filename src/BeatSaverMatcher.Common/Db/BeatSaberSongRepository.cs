using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Common.Db
{
    public class BeatSaberSongRepository : SqlServerRepository, IBeatSaberSongRepository
    {
        public BeatSaberSongRepository(IOptions<DbConfiguration> options)
            : base(options)
        {
        }

        public async Task<IList<BeatSaberSong>> GetMatches(string artistName, string trackName, bool allowAutomapped)
        {
            artistName = "\"" + new string(artistName.Where(x => x != '"' && x != '*').ToArray()) + "\"";
            trackName = "\"" + new string(trackName.Where(x => x != '"' && x != '*').ToArray()) + "\"";
            using (var connection = GetConnection())
            {
                var query = allowAutomapped ?
                    "SELECT * FROM [dbo].[BeatSaberSong] WHERE CONTAINS(*, @ArtistName) AND CONTAINS(*, @TrackName)" :
                    "SELECT * FROM [dbo].[BeatSaberSong] WHERE CONTAINS(*, @ArtistName) AND CONTAINS(*, @TrackName) AND AutoMapper IS NULL";

                var results = await connection.QueryAsync<BeatSaberSong>(query, new { ArtistName = artistName, TrackName = trackName });
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
        ([BeatSaverKey],[Hash],[Uploader],[Uploaded],[Difficulties],[Bpm],[LevelAuthorName],[SongAuthorName],[SongName],[SongSubName],[Name],[AutoMapper])
        VALUES (@BeatSaverKey, @Hash, @Uploader, @Uploaded, @Difficulties, @Bpm, @LevelAuthorName, @SongAuthorName, @SongName, @SongSubName, @Name, @AutoMapper)";

                await connection.ExecuteAsync(sqlStr, song);
            }
        }

        public async Task<IList<(bool AutoMapper, SongDifficulties Difficulties, int Count)>> GetSongCount()
        {
            using var connection = GetConnection();
            var query = "SELECT CAST(IIF(AutoMapper IS NOT NULL, '1', '0') AS bit) AS AutoMapper, Difficulties, COUNT(*) AS Count FROM [dbo].[BeatSaberSong] GROUP BY AutoMapper, Difficulties";

            var results = await connection.QueryAsync<(bool AutoMapper, SongDifficulties Difficulties, int Count)>(query);
            return results.ToList();
        }
    }
}
