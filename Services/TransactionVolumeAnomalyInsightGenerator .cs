using Dapper;
using FinancialIntelligence.Api.Models;
using Microsoft.Data.SqlClient;


namespace FinancialIntelligence.Api.Services.Insights;

public class TransactionVolumeAnomalyInsightGenerator : IInsightGenerator
{
    private readonly string _connectionString;

    public TransactionVolumeAnomalyInsightGenerator(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("FinIntelConnection")
            ?? throw new InvalidOperationException("Missing FinIntelConnection.");
    }

    public async Task<IReadOnlyList<InsightRecord>> GenerateAsync(
        Guid loadId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);

        var sql = """
            DECLARE @MaxDate date =
            (
                SELECT MAX(CAST(TransactionDate AS date))
                FROM dbo.Transactions
                WHERE BusinessId = @BusinessId
            );

            IF @MaxDate IS NULL
            BEGIN
                SELECT
                    CAST(0 AS decimal(18,2)) AS CurrentPeriodTotal,
                    CAST(0 AS decimal(18,2)) AS PriorPeriodTotal;
                RETURN;
            END

            SELECT
                SUM(CASE
                    WHEN TransactionDate > DATEADD(day, -30, @MaxDate)
                     AND TransactionDate <= @MaxDate
                    THEN ABS(Amount) ELSE 0 END) AS CurrentPeriodTotal,
                SUM(CASE
                    WHEN TransactionDate > DATEADD(day, -60, @MaxDate)
                     AND TransactionDate <= DATEADD(day, -30, @MaxDate)
                    THEN ABS(Amount) ELSE 0 END) AS PriorPeriodTotal
            FROM dbo.Transactions
            WHERE BusinessId = @BusinessId;
            """;

        var row = await connection.QueryFirstAsync(sql, new { BusinessId = businessId });

        decimal currentTotal = row.CurrentPeriodTotal ?? 0m;
        decimal priorTotal = row.PriorPeriodTotal ?? 0m;

        if (priorTotal <= 0)
            return Array.Empty<InsightRecord>();

        var pctChange = (currentTotal - priorTotal) / priorTotal;

        if (Math.Abs(pctChange) < 0.25m)
            return Array.Empty<InsightRecord>();

        var direction = pctChange > 0 ? "increased" : "decreased";
        var severity = Math.Abs(pctChange) >= 0.50m ? "High" : "Medium";

        var insight = new InsightRecord
        {
            InsightId = Guid.NewGuid(),
            LoadId = loadId,
            BusinessId = businessId,
            InsightType = "transaction_volume_anomaly",
            Severity = severity,
            Title = "Significant transaction volume change",
            Description = $"Transaction volume has {direction} by {Math.Abs(pctChange):P0} compared with the prior 30-day period.",
            ImpactLabel = $"{Math.Abs(pctChange):P0} change",
            ImpactValue = Math.Round(Math.Abs(pctChange) * 100m, 2),
            Recommendation = pctChange > 0
                ? "Review major merchants and categories to confirm whether the increase is expected."
                : "Investigate whether the decline reflects seasonality, fewer sales, or missing transaction data.",
            CreatedAtUtc = DateTime.UtcNow
        };

        return new[] { insight };
    }
}