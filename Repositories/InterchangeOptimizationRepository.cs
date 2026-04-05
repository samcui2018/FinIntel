using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using FinancialIntelligence.Api.Models.Intelligence;

namespace FinancialIntelligence.Api.Repositories;

public sealed class InterchangeOptimizationRepository : IInterchangeOptimizationRepository
{
    private readonly string _connectionString;

    public InterchangeOptimizationRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("FinIntelConnection")
            ?? throw new InvalidOperationException("Connection string 'FinIntelConnection' was not found.");
    }

    public async Task<InterchangeOptimizationSnapshot> GetSnapshotAsync(
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var grid = await connection.QueryMultipleAsync(
            new CommandDefinition(
                "dbo.spGetInterchangeOptimizationSnapshot",
                new
                {
                    BusinessId = businessId,
                    MonthsBack = monthsBack
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var ecommerce = await grid.ReadSingleAsync<EcommerceOptimizationCandidate>();
        var level23 = await grid.ReadSingleAsync<Level23OptimizationCandidate>();
        var smallTicket = await grid.ReadSingleAsync<SmallTicketOptimizationCandidate>();
        var manualEntry = await grid.ReadSingleAsync<ManualEntryOptimizationCandidate>();

        return new InterchangeOptimizationSnapshot
        {
            Ecommerce = ecommerce,
            Level23 = level23,
            SmallTicket = smallTicket,
            ManualEntry = manualEntry
        };
    }
}