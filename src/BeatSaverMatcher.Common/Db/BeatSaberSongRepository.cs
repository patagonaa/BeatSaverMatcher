using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Common.Db
{
    public class BeatSaberSongRepository : SqlServerRepository, IBeatSaberSongRepository
    {
        public BeatSaberSongRepository(IOptions<DbConfiguration> options)
            : base(options)
        {
            SqlMapper.AddTypeMap(typeof(DateTime), DbType.DateTime2); // use datetime2 so datetimes are saved with max. precision
        }

        public async Task<IList<BeatSaberSongWithRatings>> GetMatches(string artistName, string trackName)
        {
            artistName = "\"" + new string(artistName.Where(x => x != '"' && x != '*').ToArray()) + "\"";
            trackName = "\"" + new string(trackName.Where(x => x != '"' && x != '*').ToArray()) + "\"";
            using (var connection = GetConnection())
            {
                var query =
                    @"
SELECT
	song.*,
	rating.Upvotes,
	rating.Downvotes,
	rating.Score
FROM [BeatSaberSong] song
LEFT JOIN [BeatSaberSongRatings] rating ON song.[BeatSaverKey] = rating.[BeatSaverKey]
WHERE [DeletedAt] IS NULL AND
    CONTAINS(song.*, @ArtistName) AND
    CONTAINS(song.*, @TrackName) AND
    song.AutoMapper IS NULL";

                var results = await connection.QueryAsync<BeatSaberSongWithRatings>(query, new { ArtistName = artistName, TrackName = trackName });
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

        public async Task<bool> InsertOrUpdateSong(BeatSaberSong song)
        {
            using (var connection = GetConnection())
            {
                var sqlUpdate = @"
    UPDATE [dbo].[BeatSaberSong]
    SET
        [Hash] = @Hash,
        [Uploader] = @Uploader,
        [Uploaded] = @Uploaded,
        [Difficulties] = @Difficulties,
        [Bpm] = @Bpm,
        [LevelAuthorName] = @LevelAuthorName,
        [SongAuthorName] = @SongAuthorName,
        [SongName] = @SongName,
        [SongSubName] = @SongSubName,
        [Name] = @Name,
        [AutoMapper] = @AutoMapper,
        [CreatedAt] = @CreatedAt,
        [UpdatedAt] = @UpdatedAt,
        [LastPublishedAt] = @LastPublishedAt,
        [DeletedAt] = NULL
    WHERE [BeatSaverKey] = @BeatSaverKey
";

                var sqlInsert = @"
    INSERT INTO [dbo].[BeatSaberSong]
        ([BeatSaverKey],[Hash],[Uploader],[Uploaded],[Difficulties],[Bpm],[LevelAuthorName],[SongAuthorName],[SongName],[SongSubName],[Name],[AutoMapper],[CreatedAt],[UpdatedAt],[LastPublishedAt])
        VALUES (@BeatSaverKey, @Hash, @Uploader, @Uploaded, @Difficulties, @Bpm, @LevelAuthorName, @SongAuthorName, @SongName, @SongSubName, @Name, @AutoMapper, @CreatedAt, @UpdatedAt, @LastPublishedAt)
";

                bool inserted = false;
                using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    if (await connection.ExecuteAsync(sqlUpdate, song, transaction) == 0)
                    {
                        await connection.ExecuteAsync(sqlInsert, song, transaction);
                        inserted = true;
                    }
                    transaction.Commit();
                }
                return inserted;
            }
        }

        public async Task<bool> InsertOrUpdateSongRatings(BeatSaberSongRatings ratings)
        {
            using (var connection = GetConnection())
            {
                var sqlUpdate = @"
    UPDATE [dbo].[BeatSaberSongRatings]
    SET
        [Upvotes] = @Upvotes,
        [Downvotes] = @Downvotes,
        [Score] = @Score,
        [UpdatedAt] = @UpdatedAt
    WHERE [BeatSaverKey] = @BeatSaverKey
";

                var sqlInsert = @"
    INSERT INTO [dbo].[BeatSaberSongRatings]
        ([BeatSaverKey],[Upvotes],[Downvotes],[Score],[UpdatedAt])
        VALUES (@BeatSaverKey,@Upvotes,@Downvotes,@Score,@UpdatedAt)
";

                bool inserted = false;
                using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    if (await connection.ExecuteAsync(sqlUpdate, ratings, transaction) == 0)
                    {
                        await connection.ExecuteAsync(sqlInsert, ratings, transaction);
                        inserted = true;
                    }
                    transaction.Commit();
                }
                return inserted;
            }
        }

        public async Task MarkDeleted(int key, DateTime deletedAt)
        {
            using var connection = GetConnection();
            var query = "UPDATE [dbo].[BeatSaberSong] SET DeletedAt = @DeletedAt WHERE BeatSaverKey = @BeatSaverKey";

            await connection.ExecuteAsync(query, new { DeletedAt = deletedAt, BeatSaverKey = key});
        }

        public async Task<IList<(bool AutoMapper, SongDifficulties Difficulties, int Count)>> GetSongCount()
        {
            using var connection = GetConnection();
            var query = "SELECT CAST(IIF(AutoMapper IS NOT NULL, '1', '0') AS bit) AS AutoMapper, Difficulties, COUNT(*) AS Count FROM [dbo].[BeatSaberSong] GROUP BY AutoMapper, Difficulties";

            var results = await connection.QueryAsync<(bool AutoMapper, SongDifficulties Difficulties, int Count)>(query);
            return results.ToList();
        }

        public async Task<DateTime?> GetLatestUpdatedAt(CancellationToken token)
        {
            using (var connection = GetConnection())
            {
                var sqlStr = @"SELECT TOP 1 [UpdatedAt] FROM [dbo].[BeatSaberSong] ORDER BY [UpdatedAt] DESC";

                return await connection.QueryFirstOrDefaultAsync<DateTime?>(sqlStr);
            }
        }

        public async Task<DateTime?> GetLatestDeletedAt(CancellationToken token)
        {
            using (var connection = GetConnection())
            {
                var sqlStr = @"SELECT TOP 1 [DeletedAt] FROM [dbo].[BeatSaberSong] ORDER BY [DeletedAt] DESC";

                return await connection.QueryFirstOrDefaultAsync<DateTime?>(sqlStr);
            }
        }

        public async Task<DateTime?> GetLatestScoreUpdatedAt(CancellationToken token)
        {
            using (var connection = GetConnection())
            {
                var sqlStr = @"SELECT TOP 1 [UpdatedAt] FROM [dbo].[BeatSaberSongRatings] ORDER BY [UpdatedAt] DESC";

                return await connection.QueryFirstOrDefaultAsync<DateTime?>(sqlStr);
            }
        }
    }
}
