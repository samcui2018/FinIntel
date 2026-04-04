using Dapper;
using FinancialIntelligence.Api.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FinancialIntelligence.Api.Repositories;

public class InsightRepository : IInsightRepository
{
    private readonly string _connectionString;

    public InsightRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("FinIntelConnection")
            ?? throw new InvalidOperationException("Missing FinIntelConnection.");
    }

   public async Task BuildInsightsAsync(
    IReadOnlyList<InsightRecord> insights,
    CancellationToken cancellationToken = default)
    {
        if (insights.Count == 0)
        {
            return;
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var table = BuildInsightTable(insights);

            var parameters = new DynamicParameters();
            parameters.Add(
                "@Insights",
                table.AsTableValuedParameter("dbo.InsightTableType"));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "dbo.spInsightsBulkInsert",
                    parameters,
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
    
    public async Task<IReadOnlyList<InsightRecord>> GetByBusinessIdAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);

        // var sql = """
        //     SELECT
        //        distinct
        //         --LoadId,
        //         BusinessId,
        //         InsightType,
        //         Severity,
        //         Title,
        //         Description,
        //         ImpactLabel,
        //         ImpactValue,
        //         Recommendation,
        //         ConfidenceScore
        //     FROM dbo.Insights
        //     WHERE BusinessId = @BusinessId
        //     --ORDER BY CreatedAtUtc DESC
        //     """;

        // var results = await connection.QueryAsync<InsightRecord>(
        //     new CommandDefinition(sql, new { BusinessId = businessId }, cancellationToken: cancellationToken));
         var results = await connection.QueryAsync<InsightRecord>(
            new CommandDefinition(
                "spInsightsGetByBusinessId",              // stored procedure name
                new { BusinessId = businessId },             // parameters
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));


        return results.ToList();
    }

    public async Task<IReadOnlyList<InsightRecord>> GetByLoadIdAsync(
        Guid loadId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);

        // var sql = """
        //     spInsightsGetByLoadId
        //     """;

        // var results = await connection.QueryAsync<InsightRecord>(
        //     new CommandDefinition(sql, new { LoadId = loadId }, cancellationToken: cancellationToken));
        var results = await connection.QueryAsync<InsightRecord>(
            new CommandDefinition(
                "spInsightsGetByLoadId",              // stored procedure name
                new { LoadId = loadId },             // parameters
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        return results.ToList();
    }

    private static DataTable BuildInsightTable(IEnumerable<InsightRecord> insights)
    {
        var table = new DataTable();

        table.Columns.Add("InsightId", typeof(Guid));
        table.Columns.Add("LoadId", typeof(Guid));
        table.Columns.Add("BusinessId", typeof(Guid));
        table.Columns.Add("InsightType", typeof(string));
        table.Columns.Add("Severity", typeof(string));
        table.Columns.Add("Title", typeof(string));
        table.Columns.Add("Description", typeof(string));
        table.Columns.Add("ImpactLabel", typeof(string));
        table.Columns.Add("ImpactValue", typeof(decimal));
        table.Columns.Add("Recommendation", typeof(string));
        table.Columns.Add("ConfidenceScore", typeof(decimal));
        table.Columns.Add("CreatedAtUtc", typeof(DateTime));

        foreach (var insight in insights)
        {
            var row = table.NewRow();

            row["InsightId"] = insight.InsightId;
            row["LoadId"] = insight.LoadId;
            row["BusinessId"] = insight.BusinessId;
            row["InsightType"] = insight.InsightType;
            row["Severity"] = insight.Severity;
            row["Title"] = insight.Title;
            row["Description"] = (object?)insight.Description ?? DBNull.Value;
            row["ImpactLabel"] = (object?)insight.ImpactLabel ?? DBNull.Value;
            row["ImpactValue"] = insight.ImpactValue.HasValue
                ? insight.ImpactValue.Value
                : DBNull.Value;
            row["Recommendation"] = (object?)insight.Recommendation ?? DBNull.Value;
            row["ConfidenceScore"] = insight.ConfidenceScore.HasValue
                ? insight.ConfidenceScore.Value
                : DBNull.Value;
            row["CreatedAtUtc"] = insight.CreatedAtUtc;

            table.Rows.Add(row);
        }

        return table;
    }
}