using Dapper;
using Messenger.Api.Common.Database;

namespace Messenger.Api.Features.Auth;

public class AuthRepository
{
    private readonly DapperDbContext _dbConnection;

    public AuthRepository(DapperDbContext dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task CreateUserAsync(RegisterRequest request)
    {
        using var connection = _dbConnection.DbConnection;
        var sql = "WITH statement AS (" + // TODO: исключить sql инъекцию
                  $"INSERT INTO users (name, login) VALUES ('{request.Name}', '{request.DisplayName}') RETURNING id) " +
                  $"INSERT INTO secrets (user_id, secret) SELECT id, '{request.Password}' FROM statement";
        await connection.ExecuteAsync(sql);
        connection.Close();
    }
}