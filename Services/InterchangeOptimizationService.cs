using FinancialIntelligence.Api.Dtos.Analytics;
using FinancialIntelligence.Api.Models.Intelligence;
using FinancialIntelligence.Api.Repositories;

namespace FinancialIntelligence.Api.Services.Intelligence;

public sealed class InterchangeOptimizationService : IInterchangeOptimizationService
{
    private const decimal EcommerceOptimizationRateDelta = 0.0035m;
    private const decimal Level2Level3RateDelta = 0.0065m;
    private const decimal SmallTicketRateDelta = 0.0020m;
    private const decimal ManualEntryRateDelta = 0.0045m;

    private readonly IInterchangeOptimizationRepository _repository;

    public InterchangeOptimizationService(IInterchangeOptimizationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<InsightDto>> AnalyzeAsync(
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.GetSnapshotAsync(
            businessId,
            monthsBack,
            cancellationToken);

        var insights = new List<InsightDto>();

        AddEcommerceInsight(snapshot.Ecommerce, insights);
        AddLevel23Insight(snapshot.Level23, insights);
        AddSmallTicketInsight(snapshot.SmallTicket, insights);
        AddManualEntryInsight(snapshot.ManualEntry, insights);

        return insights
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.EstimatedImpact ?? 0m)
            .ToList();
    }

    private static void AddEcommerceInsight(
        EcommerceOptimizationCandidate? candidate,
        ICollection<InsightDto> insights)
    {
        if (candidate is null || candidate.TotalVolume <= 0 || candidate.AffectedVolume <= 0)
        {
            return;
        }

        var shareOfVolume = candidate.AffectedVolume / candidate.TotalVolume;
        if (shareOfVolume < 0.12m)
        {
            return;
        }

        var estimatedImpact = decimal.Round(candidate.AffectedVolume * EcommerceOptimizationRateDelta, 2);
        var confidence = shareOfVolume >= 0.30m ? 0.82m : 0.68m;

        insights.Add(new InsightDto
        {
            Type = "InterchangeOptimization",
            Title = "Improve AVS/CVV capture on card-not-present volume",
            Description =
                $"A meaningful share of spend appears to be card-not-present or e-commerce volume " +
                $"({shareOfVolume:P0} of analyzed spend, about {candidate.AffectedVolume:C0}). " +
                $"For these transactions, consistent AVS, CVV, and billing ZIP submission can help reduce downgrade risk.",
            Severity = shareOfVolume >= 0.30m ? "High" : "Medium",
            EstimatedImpact = estimatedImpact,
            Score = Score(estimatedImpact, confidence, shareOfVolume >= 0.30m ? 1.15m : 1.0m),
            Recommendation =
                $"Review gateway and processor configuration for AVS/CVV/ZIP pass-through. " +
                $"Start with merchants or channels like {DefaultMerchantText(candidate.TopMerchantsCsv)}.",
            Metrics = new Dictionary<string, object>
            {
                ["affectedTransactionCount"] = candidate.AffectedTransactionCount,
                ["affectedVolume"] = candidate.AffectedVolume,
                ["shareOfAnalyzedSpend"] = shareOfVolume,
                ["confidence"] = confidence,
                ["topMerchants"] = candidate.TopMerchantsCsv
            }
        });
    }

    private static void AddLevel23Insight(
        Level23OptimizationCandidate? candidate,
        ICollection<InsightDto> insights)
    {
        if (candidate is null || candidate.TotalVolume <= 0 || candidate.AffectedVolume <= 0)
        {
            return;
        }

        var shareOfVolume = candidate.AffectedVolume / candidate.TotalVolume;
        if (shareOfVolume < 0.10m)
        {
            return;
        }

        var estimatedImpact = decimal.Round(candidate.AffectedVolume * Level2Level3RateDelta, 2);
        var confidence = shareOfVolume >= 0.25m ? 0.84m : 0.72m;

        insights.Add(new InsightDto
        {
            Type = "InterchangeOptimization",
            Title = "Level 2/3 data may lower costs on commercial-style spend",
            Description =
                $"A notable portion of transactions looks like B2B, travel, fuel, lodging, freight, " +
                $"or other commercial-style spend ({shareOfVolume:P0} of analyzed spend, about {candidate.AffectedVolume:C0}). " +
                $"These are good candidates to review for Level 2 or Level 3 data qualification.",
            Severity = shareOfVolume >= 0.25m ? "High" : "Medium",
            EstimatedImpact = estimatedImpact,
            Score = Score(estimatedImpact, confidence, shareOfVolume >= 0.25m ? 1.20m : 1.0m),
            Recommendation =
                $"Review enhanced-data capture with your processor for invoice number, tax amount, customer code, " +
                $"PO number, and line-item detail where applicable. Focus first on {DefaultMerchantText(candidate.TopMerchantsCsv)}.",
            Metrics = new Dictionary<string, object>
            {
                ["affectedTransactionCount"] = candidate.AffectedTransactionCount,
                ["affectedVolume"] = candidate.AffectedVolume,
                ["shareOfAnalyzedSpend"] = shareOfVolume,
                ["confidence"] = confidence,
                ["topMerchants"] = candidate.TopMerchantsCsv
            }
        });
    }

    private static void AddSmallTicketInsight(
        SmallTicketOptimizationCandidate? candidate,
        ICollection<InsightDto> insights)
    {
        if (candidate is null || candidate.TotalTransactionCount <= 0 || candidate.AffectedTransactionCount <= 0)
        {
            return;
        }

        var shareOfCount = (decimal)candidate.AffectedTransactionCount / candidate.TotalTransactionCount;
        if (shareOfCount < 0.30m)
        {
            return;
        }

        var estimatedImpact = decimal.Round(candidate.AffectedVolume * SmallTicketRateDelta, 2);
        var confidence = shareOfCount >= 0.50m ? 0.78m : 0.62m;

        insights.Add(new InsightDto
        {
            Type = "InterchangeOptimization",
            Title = "Small-ticket pricing may be worth evaluating",
            Description =
                $"A large share of analyzed transactions are small-ticket items " +
                $"({shareOfCount:P0} of transactions, {candidate.AffectedTransactionCount} total, about {candidate.AffectedVolume:C0} in volume). " +
                $"Depending on MCC and processor setup, small-ticket programs may reduce effective costs.",
            Severity = shareOfCount >= 0.50m ? "Medium" : "Low",
            EstimatedImpact = estimatedImpact,
            Score = Score(estimatedImpact, confidence, shareOfCount >= 0.50m ? 1.0m : 0.85m),
            Recommendation =
                $"Check whether your merchant setup, MCC, and processor pricing qualify for small-ticket treatment. " +
                $"Representative merchants include {DefaultMerchantText(candidate.TopMerchantsCsv)}.",
            Metrics = new Dictionary<string, object>
            {
                ["affectedTransactionCount"] = candidate.AffectedTransactionCount,
                ["affectedVolume"] = candidate.AffectedVolume,
                ["shareOfTransactionCount"] = shareOfCount,
                ["smallTicketThreshold"] = candidate.ThresholdAmount,
                ["confidence"] = confidence,
                ["topMerchants"] = candidate.TopMerchantsCsv
            }
        });
    }

    private static void AddManualEntryInsight(
        ManualEntryOptimizationCandidate? candidate,
        ICollection<InsightDto> insights)
    {
        if (candidate is null || candidate.TotalVolume <= 0 || candidate.AffectedVolume <= 0)
        {
            return;
        }

        var shareOfVolume = candidate.AffectedVolume / candidate.TotalVolume;
        if (shareOfVolume < 0.05m)
        {
            return;
        }

        var estimatedImpact = decimal.Round(candidate.AffectedVolume * ManualEntryRateDelta, 2);
        var confidence = shareOfVolume >= 0.15m ? 0.80m : 0.66m;

        insights.Add(new InsightDto
        {
            Type = "InterchangeOptimization",
            Title = "Manual-entry or MOTO patterns may be increasing fees",
            Description =
                $"Some transaction descriptions suggest keyed, mail-order, telephone-order, or manually entered payments " +
                $"({shareOfVolume:P0} of analyzed spend, about {candidate.AffectedVolume:C0}). " +
                $"These patterns often carry higher processing costs than stronger card-present or optimized digital flows.",
            Severity = shareOfVolume >= 0.15m ? "High" : "Medium",
            EstimatedImpact = estimatedImpact,
            Score = Score(estimatedImpact, confidence, shareOfVolume >= 0.15m ? 1.15m : 1.0m),
            Recommendation =
                $"Where operationally possible, shift manual entry toward integrated payments, tokenized checkout, " +
                $"or stronger digital capture. Review channels associated with {DefaultMerchantText(candidate.TopMerchantsCsv)}.",
            Metrics = new Dictionary<string, object>
            {
                ["affectedTransactionCount"] = candidate.AffectedTransactionCount,
                ["affectedVolume"] = candidate.AffectedVolume,
                ["shareOfAnalyzedSpend"] = shareOfVolume,
                ["confidence"] = confidence,
                ["topMerchants"] = candidate.TopMerchantsCsv
            }
        });
    }

    private static string DefaultMerchantText(string? merchants)
        => string.IsNullOrWhiteSpace(merchants) ? "your highest-volume merchants" : merchants;

    private static decimal Score(decimal estimatedImpact, decimal confidence, decimal severityWeight)
    {
        var normalizedImpact = Math.Min(estimatedImpact / 1000m, 10m);
        return decimal.Round((normalizedImpact * 10m) * confidence * severityWeight, 2);
    }
}