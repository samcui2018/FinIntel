using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using FinancialIntelligence.Api.Models;

namespace FinancialIntelligence.Api.Repositories;

public sealed class TransactionQueryRepository : ITransactionQueryRepository
{
    private readonly string _connectionString;

    public TransactionQueryRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("FinIntelConnection")
            ?? throw new InvalidOperationException("Connection string 'FinIntelConnection' was not found.");
    }

    public async Task<IReadOnlyList<TransactionRecord>> GetByLoadIdAsync(
        Guid loadId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        return await DbRetryHelper.ExecuteWithRetryAsync(async ct =>
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var results = await connection.QueryAsync<TransactionRecord>(
                new CommandDefinition(
                    "dbo.spTransactionsGetByLoadId",
                    new { LoadId = loadId, BusinessId = businessId },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));

            return results.AsList();
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<TransactionRecord>> GetRecentTransactionsForBusinessAsync(
        Guid businessId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        return await DbRetryHelper.ExecuteWithRetryAsync(async ct =>
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var results = await connection.QueryAsync<TransactionRecord>(
                new CommandDefinition(
                    "dbo.spTransactionsGetRecentForBusiness",
                    new { BusinessId = businessId, FromUtc = fromUtc, ToUtc = toUtc },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));

            return results.AsList();
        }, cancellationToken);
    }
}