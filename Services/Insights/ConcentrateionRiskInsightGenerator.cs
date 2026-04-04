using Dapper;
using FinancialIntelligence.Api.Models;
using Microsoft.Data.SqlClient;

namespace FinancialIntelligence.Api.Services.Insights;

public class ConcentrationRiskInsightGenerator : IInsightGenerator
{
    private readonly string _connectionString;

    public ConcentrationRiskInsightGenerator(IConfiguration configuration)
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
            WITH MerchantTotals AS
            (
                SELECT
                    ISNULL(NULLIF(LTRIM(RTRIM(MerchantName)), ''), 'Unknown') AS MerchantName,
                    SUM(ABS(Amount)) AS TotalAmount
                FROM dbo.Transactions
                WHERE BusinessId = @BusinessId
                GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(MerchantName)), ''), 'Unknown')
            )
            SELECT TOP 1
                MerchantName,
                TotalAmount,
                (SELECT SUM(TotalAmount) FROM MerchantTotals) AS GrandTotal
            FROM MerchantTotals
            ORDER BY TotalAmount DESC
            """;

        var row = await connection.QueryFirstOrDefaultAsync(sql, new { BusinessId = businessId });

        if (row is null || row.GrandTotal == null || row.GrandTotal == 0)
            return Array.Empty<InsightRecord>();

        decimal merchantTotal = row.TotalAmount;
        decimal grandTotal = row.GrandTotal;
        decimal percent = grandTotal == 0 ? 0 : merchantTotal / grandTotal;

        if (percent < 0.35m)
            return Array.Empty<InsightRecord>();

        var severity = percent >= 0.60m ? "High"
            : percent >= 0.45m ? "Medium"
            : "Low";

        var insight = new InsightRecord
        {
            InsightId = Guid.NewGuid(),
            LoadId = loadId,
            BusinessId = businessId,
            InsightType = "concentration_risk",
            Severity = severity,
            Title = "High merchant concentration detected",
            Description = $"{row.MerchantName} accounts for {percent:P0} of total transaction volume.",
            ImpactLabel = $"{percent:P0} concentration",
            ImpactValue = Math.Round(percent * 100m, 2),
            Recommendation = "Review dependency on this merchant or category and diversify where possible.",
            CreatedAtUtc = DateTime.UtcNow
        };

        return new[] { insight };
    }
}