using FinancialIntelligence.Api.Models;
using FinancialIntelligence.Api.Repositories;

namespace FinancialIntelligence.Api.Services.Insights;

public sealed class InterchangeOptimizationInsightGenerator : IInsightGenerator
{
    private const decimal HighTicketThreshold = 500m;
    private const decimal EstimatedSavingsRate = 0.006m; // 0.6%
    private const int MinimumTransactionCount = 3;

    private readonly ITransactionQueryRepository _transactionRepository;

    public InterchangeOptimizationInsightGenerator(
        ITransactionQueryRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<IReadOnlyList<InsightRecord>> GenerateAsync(
        Guid loadId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        var transactions = await _transactionRepository.GetByLoadIdAsync(loadId, businessId, cancellationToken);
        if (transactions is null || transactions.Count == 0)
        {
            return Array.Empty<InsightRecord>();
        }

        var insights = transactions
            .Where(t =>
                t.BusinessId == businessId &&
                !string.IsNullOrWhiteSpace(t.MerchantName) &&
                t.Amount > 0)
            .GroupBy(t => t.MerchantName.Trim())
            .Select(group =>
            {
                var transactionCount = group.Count();
                var totalVolume = group.Sum(x => x.Amount);
                var averageTicket = group.Average(x => x.Amount);

                return new
                {
                    MerchantName = group.Key,
                    TransactionCount = transactionCount,
                    TotalVolume = totalVolume,
                    AverageTicket = averageTicket
                };
            })
            .Where(x =>
                x.TransactionCount >= MinimumTransactionCount &&
                x.AverageTicket >= HighTicketThreshold)
            .OrderByDescending(x => x.TotalVolume)
            .Select(x =>
            {
                var estimatedSavings = Decimal.Round(
                    x.TotalVolume * EstimatedSavingsRate,
                    2,
                    MidpointRounding.AwayFromZero);

                return new InsightRecord
                {
                    InsightId = Guid.NewGuid(),
                    LoadId = loadId,
                    BusinessId = businessId,
                    InsightType = "InterchangeOptimization",
                    Severity = GetSeverity(x.TotalVolume, x.AverageTicket),
                    Title = $"Reduce processing fees for {x.MerchantName}",
                    Description =
                        $"Average ticket size is ${x.AverageTicket:N2} across {x.TransactionCount} transactions " +
                        $"with total volume of ${x.TotalVolume:N2}. This pattern may indicate an opportunity " +
                        $"to optimize interchange qualification and reduce processing costs.",
                    ImpactLabel = "Estimated Savings Opportunity",
                    ImpactValue = estimatedSavings,
                    Recommendation =
                        "Review processor setup, transaction data quality, and Level 2/Level 3 data eligibility " +
                        "for this merchant category or payment flow.",
                    ConfidenceScore = GetConfidenceScore(x.TransactionCount, x.TotalVolume, x.AverageTicket),
                    CreatedAtUtc = DateTime.UtcNow
                };
            })
            .Cast<InsightRecord>()
            .ToList();

        return insights;
    }

    private static string GetSeverity(decimal totalVolume, decimal averageTicket)
    {
        if (totalVolume >= 50000m || averageTicket >= 1500m)
        {
            return "High";
        }

        if (totalVolume >= 10000m || averageTicket >= 750m)
        {
            return "Medium";
        }

        return "Low";
    }

    private static decimal GetConfidenceScore(
        int transactionCount,
        decimal totalVolume,
        decimal averageTicket)
    {
        decimal score = 0.55m;

        if (transactionCount >= 5)
        {
            score += 0.10m;
        }

        if (transactionCount >= 10)
        {
            score += 0.05m;
        }

        if (totalVolume >= 10000m)
        {
            score += 0.10m;
        }

        if (totalVolume >= 50000m)
        {
            score += 0.05m;
        }

        if (averageTicket >= 750m)
        {
            score += 0.05m;
        }

        if (averageTicket >= 1500m)
        {
            score += 0.05m;
        }

        return Math.Min(score, 0.90m);
    }
}