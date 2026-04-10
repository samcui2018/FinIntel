using FinancialIntelligence.Api.Models;
using FinancialIntelligence.Api.Repositories;
namespace FinancialIntelligence.Api.Services.Insights;

public sealed class RecurringSpendInsightGenerator : IInsightGenerator
{
    private readonly ITransactionQueryRepository _transactionQueryRepository;

    public RecurringSpendInsightGenerator(
        ITransactionQueryRepository transactionQueryRepository)
    {
        _transactionQueryRepository = transactionQueryRepository;
    }

    public async Task<IReadOnlyList<InsightRecord>> GenerateAsync(
        Guid loadId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        var toUtc = DateTime.UtcNow;
        var fromUtc = toUtc.AddDays(-90);

        var transactions = await _transactionQueryRepository.GetRecentTransactionsForBusinessAsync(
            businessId,
            fromUtc,
            toUtc,
            cancellationToken);

        if (transactions.Count == 0)
        {
            return Array.Empty<InsightRecord>();
        }

        var candidates = FindRecurringCandidates(businessId, transactions);

        return candidates
            .Select(candidate => CreateInsight(loadId, candidate))
            .ToList();
    }

    private static List<RecurringSpendCandidate> FindRecurringCandidates(
        Guid businessId,
        IReadOnlyList<TransactionRecord> transactions)
    {
        var groups = transactions
            .Where(t => t.BusinessId == businessId)
            .Where(t => !string.IsNullOrWhiteSpace(t.NormalizedMerchantName) || !string.IsNullOrWhiteSpace(t.MerchantName))
            .Where(t => Math.Abs(t.Amount) > 0)
            .GroupBy(t => GetMerchantKey(t))
            .Where(g => g.Count() >= 3);

        var candidates = new List<RecurringSpendCandidate>();

        foreach (var group in groups)
        {
            var ordered = group
                .OrderBy(t => t.TransactionDate)
                .ToList();

            if (!HasConsistentAmounts(ordered))
            {
                continue;
            }

            var frequency = DetectFrequency(ordered);
            if (frequency is null)
            {
                continue;
            }

            candidates.Add(new RecurringSpendCandidate
            {
                BusinessId = businessId,
                MerchantName = group.First().NormalizedMerchantName ?? group.First().MerchantName,
                TransactionCount = ordered.Count,
                AverageAmount = Math.Round(ordered.Average(t => Math.Abs(t.Amount)), 2),
                Frequency = frequency,
                FirstTransactionDate = ordered.First().TransactionDate,
                LastTransactionDate = ordered.Last().TransactionDate
            });
        }

        return candidates;
    }

    private static string GetMerchantKey(TransactionRecord transaction)
    {
        return (transaction.NormalizedMerchantName ?? transaction.MerchantName).Trim().ToUpperInvariant();
    }

    private static bool HasConsistentAmounts(List<TransactionRecord> transactions)
    {
        var amounts = transactions
            .Select(t => Math.Abs(t.Amount))
            .OrderBy(a => a)
            .ToList();

        if (amounts.Count < 3)
        {
            return false;
        }

        var average = amounts.Average();
        if (average <= 0)
        {
            return false;
        }

        foreach (var amount in amounts)
        {
            var percentDiff = Math.Abs(amount - average) / average;
            var absoluteDiff = Math.Abs(amount - average);

            if (percentDiff > 0.05m && absoluteDiff > 2.00m)
            {
                return false;
            }
        }

        return true;
    }

    private static string? DetectFrequency(List<TransactionRecord> transactions)
    {
        if (transactions.Count < 3)
        {
            return null;
        }

        var dayDiffs = new List<int>();

        for (int i = 1; i < transactions.Count; i++)
        {
            var days = (transactions[i].TransactionDate.Date - transactions[i - 1].TransactionDate.Date).Days;
            dayDiffs.Add(days);
        }

        if (dayDiffs.All(d => d >= 28 && d <= 33))
        {
            return "Monthly";
        }

        if (dayDiffs.All(d => d >= 13 && d <= 15))
        {
            return "Biweekly";
        }

        if (dayDiffs.All(d => d >= 6 && d <= 8))
        {
            return "Weekly";
        }

        return null;
    }

    private static InsightRecord CreateInsight(
        Guid loadId,
        RecurringSpendCandidate candidate)
    {
        var monthlyEquivalent = candidate.Frequency switch
        {
            "Weekly" => Math.Round(candidate.AverageAmount * 4.33m, 2),
            "Biweekly" => Math.Round(candidate.AverageAmount * 2.17m, 2),
            _ => candidate.AverageAmount
        };

        return new InsightRecord
        {
            InsightId = Guid.NewGuid(),
            LoadId = loadId,
            BusinessId = candidate.BusinessId,
            InsightType = "recurring_spend",
            Severity = monthlyEquivalent >= 500 ? "High" : "Medium",
            Title = $"Recurring charges detected at {candidate.MerchantName}",
            Description =
                $"{candidate.TransactionCount} recurring charges were detected for {candidate.MerchantName} " +
                $"from {candidate.FirstTransactionDate:MMM d, yyyy} to {candidate.LastTransactionDate:MMM d, yyyy}, " +
                $"averaging {candidate.AverageAmount:C} on a {candidate.Frequency.ToLowerInvariant()} basis.",
            ImpactLabel = $"{monthlyEquivalent:C} / month",
            ImpactValue = monthlyEquivalent,
            Recommendation = "Review this recurring expense to confirm it is still needed and priced appropriately.",
            ConfidenceScore = 0.88m,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}