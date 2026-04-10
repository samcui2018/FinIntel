using Dapper;
using FinancialIntelligence.Api.Models;
using Microsoft.Data.SqlClient;

namespace FinancialIntelligence.Api.Services;

public class DuplicateChargeInsightGenerator : IInsightGenerator
{
    private readonly string _connectionString;

    public DuplicateChargeInsightGenerator(IConfiguration configuration)
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
            WITH PotentialDuplicates AS
            (
                SELECT
                    ISNULL(NULLIF(LTRIM(RTRIM(MerchantName)), ''), 'Unknown') AS MerchantName,
                    ABS(Amount) AS AmountAbs,
                    CAST(TransactionDate AS date) AS TxnDate,
                    COUNT(*) AS Cnt
                FROM dbo.Transactions
                WHERE BusinessId = @BusinessId
                GROUP BY
                    ISNULL(NULLIF(LTRIM(RTRIM(MerchantName)), ''), 'Unknown'),
                    ABS(Amount),
                    CAST(TransactionDate AS date)
                HAVING COUNT(*) > 1
            )
            SELECT
                MerchantName,
                COUNT(*) AS DuplicateGroups,
                SUM(Cnt) AS DuplicateTransactions
            FROM PotentialDuplicates group by MerchantName
            """;

        var row = await connection.QueryFirstOrDefaultAsync(sql, new { BusinessId = businessId });

        int groups = row?.DuplicateGroups ?? 0;
        int txns = row?.DuplicateTransactions ?? 0;
        var merchantName = row?.MerchantName ?? "Unknown";

        if (groups == 0)
            return Array.Empty<InsightRecord>();

        var severity = groups >= 5 ? "High"
            : groups >= 2 ? "Medium"
            : "Low";

        var insight = new InsightRecord
        {
            InsightId = Guid.NewGuid(),
            LoadId = loadId,
            BusinessId = businessId,
            InsightType = "duplicate_charge_risk",
            Severity = severity,
            Title = "Potential duplicate charges detected",
            Description = $"{groups} duplicate transaction group(s) were identified for merchant '{merchantName}' across {txns} transactions.",
            ImpactLabel = $"{groups} duplicate groups", 
            // Description = $"{groups} duplicate transaction group(s) were identified across {txns} transactions.",
            // ImpactLabel = $"{groups} duplicate groups",
            ImpactValue = groups,
            Recommendation = "Review these transactions for duplicate processing, subscription overlap, or billing errors.",
            CreatedAtUtc = DateTime.UtcNow
        };

        return new[] { insight };
    }
}