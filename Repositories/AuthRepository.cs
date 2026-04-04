using FinancialIntelligence.Api.Models;
using Microsoft.Data.SqlClient;

namespace FinancialIntelligence.Api.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly string _connectionString;

    public AuthRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("FinIntelConnection")
            ?? throw new InvalidOperationException("Connection string 'FinIntelConnection' was not found.");
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT UserId, Email, PasswordHash, Role, CreatedAt
            FROM dbo.Users
            WHERE Email = @Email;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Email", email.Trim());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new User
        {
            UserId = reader.GetGuid(0),
            Email = reader.GetString(1),
            PasswordHash = reader.GetString(2),
            Role = reader.GetString(3),
            //BusinessId = Convert.ToString(reader.GetInt32(4)) ?? "",
            CreatedAt = reader.GetDateTime(4)
        };
    }

    public async Task<Guid> CreateUserAsync(
        string email,
        string passwordHash,
        string role = "User",
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO dbo.Users (Email, PasswordHash, Role)
            OUTPUT inserted.UserId
            VALUES (@Email, @PasswordHash, @Role);";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Email", email.Trim());
        command.Parameters.AddWithValue("@PasswordHash", passwordHash);
        command.Parameters.AddWithValue("@Role", role);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result is null || result == DBNull.Value)
        {
            throw new InvalidOperationException("Failed to create user.");
        }

        return (Guid)result;
    }
}