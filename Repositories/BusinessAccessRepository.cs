using Dapper;
using FinancialIntelligence.Api.Dtos.Auth;
using Microsoft.Data.SqlClient;

namespace FinancialIntelligence.Api.Repositories;

public class BusinessAccessRepository : IBusinessAccessRepository
{
    private readonly string _connectionString;

    public BusinessAccessRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("FinIntelConnection")
            ?? throw new InvalidOperationException("Missing DefaultConnection");
    }

    public async Task<List<UserBusinessDto>> GetBusinessesForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
        SELECT
            b.BusinessId,
            b.BusinessKey,
            b.BusinessName,
            ub.RoleName,
            ub.IsDefault
        FROM dbo.UserBusinesses ub
        INNER JOIN dbo.Businesses b
            ON ub.BusinessId = b.BusinessId
        WHERE ub.UserId = @UserId
          AND b.IsActive = 1
        ORDER BY ub.IsDefault DESC, b.BusinessName ASC;
        """;

        await using var connection = new SqlConnection(_connectionString);
        var result = await connection.QueryAsync<UserBusinessDto>(new CommandDefinition(
            sql,
            new { UserId = userId },
            cancellationToken: cancellationToken));

        return result.ToList();
    }

    public async Task<bool> UserHasAccessToBusinessAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
        SELECT COUNT(1)
        FROM dbo.UserBusinesses
        WHERE UserId = @UserId
          AND BusinessId = @BusinessId;
        """;

        await using var connection = new SqlConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            sql,
            new { UserId = userId, BusinessId = businessId },
            cancellationToken: cancellationToken));

        return count > 0;
    }

    public async Task SetDefaultBusinessAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
        BEGIN TRANSACTION;

        UPDATE dbo.UserBusinesses
        SET IsDefault = 0
        WHERE UserId = @UserId;

        UPDATE dbo.UserBusinesses
        SET IsDefault = 1
        WHERE UserId = @UserId
          AND BusinessId = @BusinessId;

        COMMIT TRANSACTION;
        """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { UserId = userId, BusinessId = businessId },
            cancellationToken: cancellationToken));
    }
}