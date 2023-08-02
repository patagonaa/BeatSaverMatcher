using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace BeatSaverMatcher.Common
{
    public abstract class SqlServerRepository
    {
        private readonly DbConfiguration _config;

        protected SqlServerRepository(IOptions<DbConfiguration> options)
        {
            _config = options.Value;
        }

        protected SqlConnection GetConnection()
        {
            SqlConnection sqlConnection = new SqlConnection(_config.ConnectionString);
            sqlConnection.Open();
            return sqlConnection;
        }
    }
}
