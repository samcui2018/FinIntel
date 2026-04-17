using FinancialIntelligence.Api.Repositories;
using FinancialIntelligence.Api.Models;
using FinancialIntelligence.Api.Services.Insights;

namespace FinancialIntelligence.Api.Services.Insights;
public sealed class SubscriptionWasteInsightGenerator : IInsightGenerator
{
    private readonly ITransactionQueryRepository _transactionRepository;

    public SubscriptionWasteInsightGenerator(ITransactionQueryRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<IReadOnlyList<InsightRecord>> GenerateAsync(
        Guid loadId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        var transactions = await _transactionRepository.GetByLoadIdAsync(
            loadId,
            businessId,
            cancellationToken);

        if (transactions.Count == 0)
        {
            return Array.Empty<InsightRecord>();
        }

        var insights = new List<InsightRecord>();

        var groups = transactions
            .Where(t => t.Amount > 0 && !string.IsNullOrWhiteSpace(t.MerchantName))
            .GroupBy(t => t.MerchantName.Trim());

        foreach (var group in groups)
        {
            var ordered = group
                .OrderBy(t => t.TransactionDate)
                .ToList();

            if (ordered.Count < 3)
            {
                continue;
            }

            var avgAmount = ordered.Average(x => x.Amount);
            var amountVarianceOk = ordered.All(x =>
                Math.Abs(x.Amount - avgAmount) <= Math.Max(5m, avgAmount * 0.15m));

            if (!amountVarianceOk)
            {
                continue;
            }

            var gaps = ordered
                .Zip(ordered.Skip(1), (a, b) => (b.TransactionDate - a.TransactionDate).TotalDays)
                .ToList();

            var looksMonthly = gaps.Count > 0 && gaps.All(d => d >= 20 && d <= 40);

            if (!looksMonthly)
            {
                continue;
            }

            var lastDate = ordered.Max(x => x.TransactionDate);
            var daysSinceLast = (DateTime.UtcNow.Date - lastDate.Date).TotalDays;

            if (daysSinceLast < 30)
            {
                continue;
            }

            insights.Add(new InsightRecord
            {
                InsightId = Guid.NewGuid(),
                LoadId = loadId,
                BusinessId = businessId,
                InsightType = "SubscriptionWaste",
                Severity = avgAmount >= 100m ? "Medium" : "Low",
                Title = $"Review unused subscription: {group.Key}",
                Description = $"Recurring monthly charges averaging ${avgAmount:N2} have not appeared in the last {daysSinceLast:F0} days.",
                ImpactLabel = "Potential Monthly Waste",
                ImpactValue = decimal.Round(avgAmount, 2),
                Recommendation = "Review whether this subscription is still needed and cancel it if no longer in use.",
                ConfidenceScore = 0.78m,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        return insights;
    }
}