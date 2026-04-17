using System.Text.Json;
using FinancialIntelligence.Api.Dtos.Analytics;
using FinancialIntelligence.Api.Repositories;
using FinancialIntelligence.Api.Models;

namespace FinancialIntelligence.Api.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly IInsightRanker _insightRanker;
    private readonly IInsightAnalyzer _interchangeOptimizationService;
    private readonly IInsightAnalyzer _pythonInsightGenerator;

   public AnalyticsService(
        IAnalyticsRepository analyticsRepository,
        //IInsightAnalyzer interchangeOptimizationService,
        IInsightAnalyzer pythonInsightGenerator,        
        IInsightRanker insightRanker)
    {
        _analyticsRepository = analyticsRepository;
        //_interchangeOptimizationService = interchangeOptimizationService;
        _pythonInsightGenerator = pythonInsightGenerator;
        _insightRanker = insightRanker;
    } 
    
    public Task<AnalyticsSummaryDto> GetSummaryAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default) =>
        _analyticsRepository.GetSpendSummaryAsync(userId, businessId, cancellationToken);

    public Task<IReadOnlyList<MonthlyTrendPointResponse>> GetMonthlyTrendAsync(
        Guid userId,
        Guid businessId,
        CancellationToken cancellationToken = default) =>
        _analyticsRepository.GetMonthlySpendTrendAsync(userId, businessId, cancellationToken);

    public Task<IReadOnlyList<TopMerchantResponse>> GetTopMerchantsAsync(
        Guid userId,
        Guid businessId,
        int top,
        CancellationToken cancellationToken = default) =>
        _analyticsRepository.GetTopSpendMerchantsAsync(userId, businessId, top, cancellationToken);

    public Task<IReadOnlyList<UploadHistoryItemResponse>> GetUploadHistoryAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _analyticsRepository.GetUploadHistoryAsync(userId, cancellationToken);
    public async Task<TopInsightsResponse> GetTopInsightsAsync(
    Guid businessId,
    int monthsBack,
    CancellationToken cancellationToken = default)
    {
        var monthlySpendTask = _analyticsRepository.GetMonthlySpendAsync(businessId, monthsBack, cancellationToken);
        var topMerchantTask = _analyticsRepository.GetTopMerchantSpendAsync(businessId, monthsBack, 10, cancellationToken);
        var monthlyCategoryTask = _analyticsRepository.GetMonthlyCategorySpendAsync(businessId, monthsBack, cancellationToken);
        var duplicateTask = _analyticsRepository.GetPossibleDuplicateChargesAsync(businessId, monthsBack, cancellationToken);
        // var interchangeTask = _interchangeOptimizationService.AnalyzeAsync("SpendAnomaly", businessId, monthsBack, cancellationToken);
        var pythonAnomalyTask = _pythonInsightGenerator.AnalyzeAsync("SpendAnomaly", businessId, monthsBack, cancellationToken);
        var pythonForcastTask = _pythonInsightGenerator.AnalyzeAsync("SpendForecast", businessId, monthsBack, cancellationToken);

        await Task.WhenAll(monthlySpendTask, topMerchantTask, monthlyCategoryTask, duplicateTask, pythonAnomalyTask, pythonForcastTask);

        var candidates = new List<InsightDto>();

        candidates.AddRange(BuildMonthlyTrendInsights(businessId, monthlySpendTask.Result));
        candidates.AddRange(BuildVendorConcentrationInsights(businessId, topMerchantTask.Result));
        candidates.AddRange(BuildCategorySpikeInsights(businessId, monthlyCategoryTask.Result));
        candidates.AddRange(BuildDuplicateChargeInsights(businessId, duplicateTask.Result)); 
        //candidates.AddRange(interchangeTask.Result);
        candidates.AddRange(pythonAnomalyTask.Result);
        candidates.AddRange(pythonForcastTask.Result);

  
        var insightRecords = candidates
        .Select(MapToInsightRecord)
        .ToList();

        var ranked = _insightRanker.Rank(insightRecords);

        var top5 = ranked
            .Take(5)
            .Select(MapToDto)
            .ToList();

        return new TopInsightsResponse
        {
            BusinessId = businessId,
            GeneratedAtUtc = DateTime.UtcNow,
            LookbackMonths = monthsBack,
            Insights = top5
        };
    }

    private static IReadOnlyList<InsightDto> BuildMonthlyTrendInsights(
    Guid businessId,
    IReadOnlyList<MonthlySpendDto> monthly)
    {
        if (monthly.Count < 2)
        {
            return Array.Empty<InsightDto>();
        }

        var previous = monthly[^2];
        var current = monthly[^1];

        if (previous.Amount == 0)
        {
            return Array.Empty<InsightDto>();
        }

        var delta = current.Amount - previous.Amount;
        var deltaPct = (delta / previous.Amount) * 100m;

        var severity =
            deltaPct >= 25m ? "High" :
            deltaPct >= 10m ? "Medium" :
            "Low";

        return new[]
        {
            new InsightDto
            {
                BusinessId = businessId,
                Type = "SpendTrend",
                Title = $"Monthly spend {(delta > 0 ? "increased" : "decreased")} {Math.Abs(Math.Round(deltaPct,1))}%",

                Description = $"Total spend changed from {previous.Amount:C} to {current.Amount:C} month over month.",

                Severity = severity,
                EstimatedImpact = Math.Round(delta, 2),
                ImpactLabel = delta > 0 ? "Cost Increase" : "Cost Reduction",

                Recommendation = delta > 0
                    ? "Review top contributing merchants to validate whether this increase is expected."
                    : "Evaluate whether reduced spend reflects efficiency gains or missing activity.",

                ConfidenceScore = 0.9m,
                CurrencyCode = "USD",

                Metrics = new Dictionary<string, object>
                {
                    ["previousMonth"] = previous.MonthStart.ToString("yyyy-MM"),
                    ["currentMonth"] = current.MonthStart.ToString("yyyy-MM"),
                    ["previousAmount"] = previous.Amount,
                    ["currentAmount"] = current.Amount,
                    ["changeAmount"] = delta,
                    ["changePct"] = Math.Round(deltaPct, 2)
                }
            }
        };
    }
    private static IReadOnlyList<InsightDto> BuildVendorConcentrationInsights(
    Guid businessId,
    IReadOnlyList<TopMerchantDto> merchants)
    {
        if (merchants.Count == 0)
        {
            return Array.Empty<InsightDto>();
        }

        var total = merchants.Sum(x => x.TotalAmount);
        var top = merchants[0];

        if (total == 0)
        {
            return Array.Empty<InsightDto>();
        }

        var concentrationPct = (top.TotalAmount / total) * 100m;

        if (concentrationPct < 30m)
        {
            return Array.Empty<InsightDto>();
        }

        return new[]
        {
            new InsightDto
            {
                BusinessId = businessId,
                Type = "VendorConcentration",
                Title = $"High reliance on {top.MerchantName}",

                Description = $"{top.MerchantName} accounts for {Math.Round(concentrationPct,1)}% of your total spend.",

                Severity = concentrationPct >= 50m ? "High" : "Medium",
                EstimatedImpact = top.TotalAmount,
                ImpactLabel = "Vendor Risk",

                Recommendation = "Consider diversifying vendors or renegotiating terms to reduce dependency risk.",

                ConfidenceScore = 0.85m,
                CurrencyCode = "USD",

                Metrics = new Dictionary<string, object>
                {
                    ["merchant"] = top.MerchantName,
                    ["merchantSpend"] = top.TotalAmount,
                    ["totalSpend"] = total,
                    ["concentrationPct"] = Math.Round(concentrationPct, 2)
                },
                VisualizationType = "bar",
                Visualization = InsightVisualizationFactory.CreateBarChart(
                    title: "Top merchant spend concentration",
                    labels: merchants.Select(x => x.MerchantName).ToList(),
                    new InsightVisualizationSeriesDto
                    {
                        Name = "Spend",
                        Values = merchants.Select(x => x.TotalAmount).ToList()
                    })
            }
        };
    }
    private static IReadOnlyList<InsightDto> BuildCategorySpikeInsights(
    Guid businessId,
    IReadOnlyList<CategorySpendDto> categories)
    {
        if (categories.Count == 0)
        {
            return Array.Empty<InsightDto>();
        }

        var total = categories.Sum(x => x.TotalAmount);
        var top = categories.OrderByDescending(x => x.TotalAmount).First();

        if (total == 0)
        {
            return Array.Empty<InsightDto>();
        }

        var pct = (top.TotalAmount / total) * 100m;

        if (pct < 40m)
        {
            return Array.Empty<InsightDto>();
        }

        return new[]
        {
            new InsightDto
            {
                BusinessId = businessId,
                Type = "CategoryConcentration",
                Title = $"High spend in {top.Category}",

                Description = $"{top.Category} represents {Math.Round(pct,1)}% of your total spending.",

                Severity = pct >= 60m ? "High" : "Medium",
                EstimatedImpact = top.TotalAmount,
                ImpactLabel = "Category Risk",

                Recommendation = "Review category spend for optimization opportunities or budget rebalancing.",

                ConfidenceScore = 0.8m,
                CurrencyCode = "USD",

                Metrics = new Dictionary<string, object>
                {
                    ["category"] = top.Category,
                    ["categorySpend"] = top.TotalAmount,
                    ["totalSpend"] = total,
                    ["percentage"] = Math.Round(pct, 2)
                },
                VisualizationType = "pie",
                Visualization = InsightVisualizationFactory.CreatePieChart(
                    title: "Spend by category",
                    labels: categories.Select(x => x.Category).ToList(),
                    new InsightVisualizationSeriesDto
                    {
                        Name = "Spend",
                        Values = categories.Select(x => x.TotalAmount).ToList()
                    })
            }
        };
    }
    private static IReadOnlyList<InsightDto> BuildDuplicateChargeInsights(
    Guid businessId,
    IReadOnlyList<DuplicateChargeDto> duplicates)
    {
        if (duplicates.Count == 0)
        {
            return Array.Empty<InsightDto>();
        }

        var totalImpact = duplicates.Sum(x => x.Amount);

        return new[]
        {
            new InsightDto
            {
                BusinessId = businessId,
                Type = "DuplicateCharges",
                Title = $"Potential duplicate charges detected ({duplicates.Count})",

                Description = $"Detected {duplicates.Count} potential duplicate transactions totaling {totalImpact:C}.",

                Severity = duplicates.Count > 5 ? "High" : "Medium",
                EstimatedImpact = totalImpact,
                ImpactLabel = "Recoverable Cost",

                Recommendation = "Review flagged transactions and request refunds or corrections where applicable.",

                ConfidenceScore = 0.75m,
                CurrencyCode = "USD",

                Metrics = new Dictionary<string, object>
                {
                    ["duplicateCount"] = duplicates.Count,
                    ["totalImpact"] = totalImpact
                },
                // VisualizationType = "table",
                // Visualization = InsightVisualizationFactory.CreateTable(
                //     title: "Potential duplicate transactions",
                //     columns: new[] { "Date", "Merchant", "Amount" },
                //     rows: duplicates.Select(x => new object[] { x.TransactionDate.ToString("yyyy-MM-dd"), x.MerchantName, x.Amount }).ToList()
                // )
            }
        };
    }

    private static decimal Score(decimal impactAmount, decimal relativeSignal, decimal multiplier)
    {
        var impactComponent = Math.Min(impactAmount, 50000m) / 100m;
        var signalComponent = Math.Min(relativeSignal, 100m);
        return Math.Round((impactComponent + signalComponent) * multiplier, 2);
    }
    private static InsightRecord MapToInsightRecord(InsightDto dto)
    {
        return new InsightRecord
        {
            InsightId = Guid.NewGuid(),
            BusinessId = dto.BusinessId,
            InsightType = dto.Type,
            Title = dto.Title,
            Description = dto.Description, // ✅ fixed
            Severity = dto.Severity,
            ImpactValue = dto.EstimatedImpact,
            ImpactLabel = dto.ImpactLabel,
            Recommendation = dto.Recommendation,
            ConfidenceScore = dto.ConfidenceScore ?? 0.7m,
            CurrencyCode = dto.CurrencyCode,
            MetricsJson = dto.Metrics is null ? null : JsonSerializer.Serialize(dto.Metrics),
            CreatedAtUtc = DateTime.UtcNow
        };
    }
    private static InsightDto MapToDto(RankedInsight ranked)
    {
        var i = ranked.Insight;

        return new InsightDto
        {
            Type = i.InsightType,
            Title = i.Title,
            Description = i.Description,
            Severity = i.Severity,

            ImpactLabel = i.ImpactLabel,
            EstimatedImpact = i.ImpactValue ?? 0m,

            Recommendation = i.Recommendation,
            ConfidenceScore = i.ConfidenceScore,
            Score = (decimal)ranked.PriorityScore
        };
    }
}