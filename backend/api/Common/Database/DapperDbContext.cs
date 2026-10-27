using System.Data;
using Npgsql;

namespace Messenger.Api.Common.Database;

public class DapperDbContext
{
    private readonly IDbConnection _dbConnection;
    
    public IDbConnection DbConnection => _dbConnection;
    public DapperDbContext(IConfiguration config)
    {
        var connectionString = config.GetConnectionString("ConnectionStrings:Postgres");
        _dbConnection = new NpgsqlConnection(connectionString);
    }
}