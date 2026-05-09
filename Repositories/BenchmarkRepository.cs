using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using FinancialIntelligence.Api.Models;
//using FinancialIntelligence.Api.Repositories;

namespace FinancialIntelligence.Api.Repositories;

public sealed class BenchmarkRepository : IBenchmarkRepository
{
    private readonly string _connectionString;

    public BenchmarkRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("FinIntelConnection")
            ?? throw new InvalidOperationException("Connection string 'FinIntelConnection' is missing.");
    }

    public async Task<BusinessBenchmarkContext?> GetBusinessBenchmarkContextAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            WITH MonthlySpend AS
            (
                SELECT
                    DATEFROMPARTS(YEAR(t.TransactionDate), MONTH(t.TransactionDate), 1) AS MonthStart,
                    SUM(CASE WHEN t.CountsAsSpend = 1 THEN t.AbsoluteAmount ELSE 0 END) AS MonthlySpend,
                    COUNT(CASE WHEN t.CountsAsSpend = 1 THEN 1 END) AS MonthlyTxnCount
                FROM dbo.Transactions t
                WHERE t.BusinessId = @BusinessId
                  AND t.TransactionDate >= DATEADD(MONTH, -3, CAST(GETUTCDATE() AS date))
                GROUP BY DATEFROMPARTS(YEAR(t.TransactionDate), MONTH(t.TransactionDate), 1)
            )
            SELECT
                b.BusinessId,
                b.Industry,
                CAST(AVG(CAST(ms.MonthlySpend AS decimal(18,2))) AS decimal(18,2)) AS AverageMonthlySpend,
                CAST(AVG(CAST(ms.MonthlyTxnCount AS decimal(18,2))) AS int) AS AverageMonthlyTransactionCount,
                'US' AS Geography
            FROM dbo.Businesses b
            LEFT JOIN MonthlySpend ms
                ON b.BusinessId = @BusinessId
            WHERE b.BusinessId = @BusinessId
            GROUP BY b.BusinessId, b.Industry;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            sql,
            new { BusinessId = businessId },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<BusinessBenchmarkContext>(command);
    }

    public async Task<IReadOnlyList<BenchmarkProfile>> GetBenchmarkProfilesAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                BenchmarkProfileId,
                ProfileName,
                Industry,
                SpendBand,
                TransactionBand,
                Geography,
                SourceType,
                ConfidenceScore,
                EffectiveDate,
                ISNULL(SampleSize, 0) AS SampleSize
            FROM dbo.BenchmarkProfiles
            WHERE ISNULL(IsActive, 1) = 1
            ORDER BY
                CASE WHEN Industry IS NULL OR Industry = 'General' THEN 1 ELSE 0 END,
                EffectiveDate DESC;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            sql,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<BenchmarkProfile>(command);
        return rows.ToList();
    }

    public async Task<BenchmarkMetricValue?> GetBenchmarkMetricValueAsync(
        Guid benchmarkProfileId,
        string metricKey,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                BenchmarkProfileId,
                MetricKey,
                P10,
                P25,
                P50,
                P75,
                P90,
                MeanValue,
                ISNULL(SampleSize, 0) AS SampleSize,
                SourceNote
            FROM dbo.BenchmarkMetricValues
            WHERE BenchmarkProfileId = @BenchmarkProfileId
              AND MetricKey = @MetricKey;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            sql,
            new
            {
                BenchmarkProfileId = benchmarkProfileId,
                MetricKey = metricKey
            },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<BenchmarkMetricValue>(command);
    }
}