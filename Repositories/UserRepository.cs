
using Dapper;
using FinancialIntelligence.Api.Models;
using Microsoft.Data.SqlClient;

namespace FinancialIntelligence.Api.Repositories;

public class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("FinIntelConnection")
            ?? throw new InvalidOperationException("Missing connection string");
        Console.WriteLine($"Connection string exists: {_connectionString}");
    }

    public async Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
        SELECT TOP 1
            UserId,
            Email,
            PasswordHash,
            Role,
            CreatedAt
        FROM dbo.Users
        WHERE Email = @Email;
        """;
        return await DbRetryHelper.ExecuteWithRetryAsync(async ct =>
        {
            await using var connection = new SqlConnection(_connectionString);

            var user = await connection.QueryFirstOrDefaultAsync<User>(
                new CommandDefinition(
                    sql,
                    new { Email = email },
                    cancellationToken: cancellationToken
                )
            );

            return user;
        }, cancellationToken);
    }
    public async Task<Guid> GetBusinessKeyForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
        SELECT c.BusinessId
        FROM dbo.Users a join dbo.UserBusinesses b on a.UserId = b.UserId
        join dbo.Businesses c on b.BusinessId = c.BusinessId
        WHERE a.UserId = @UserId;
        """;
        return await DbRetryHelper.ExecuteWithRetryAsync(async ct =>
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@UserId", userId);

            var result = await cmd.ExecuteScalarAsync(cancellationToken);

            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException($"No BusinessKey found for user {userId}.");

            return Guid.Parse(result.ToString());
        }, cancellationToken);
    }
}