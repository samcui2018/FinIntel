using System.Text.Json;
using FinancialIntelligence.Api.Dtos.Analytics;
using FinancialIntelligence.Api.Dtos.Intelligence;
using FinancialIntelligence.Api.Repositories;

namespace FinancialIntelligence.Api.Services;

public sealed class IntelligenceService : IIntelligenceService
{
    private readonly IInsightRepository _insightRepository;
    private readonly IAnalyticsService _analyticsService;

    private readonly IBenchmarkService _benchmarkService;
    private readonly IPredictionService _predictionService;
    private readonly IExecutiveSummaryService _executiveSummaryService;

    public IntelligenceService(
        // IInsightRepository insightRepository,
        IBenchmarkService benchmarkService,
        IPredictionService predictionService,
        IExecutiveSummaryService executiveSummaryService,
        IAnalyticsService analyticsService)
    {
        // _insightRepository = insightRepository;
        _benchmarkService = benchmarkService;
        _predictionService = predictionService;
        _executiveSummaryService = executiveSummaryService;
        _analyticsService = analyticsService;
    }

    public async Task<IntelligenceResponse> GetIntelligenceAsync(
        Guid loadId,
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default)
    {
        var benchmarkTask = _benchmarkService.CompareAsync(
            loadId,
            businessId,
            monthsBack,
            cancellationToken);

        var forecastTask = _predictionService.ForecastMonthlySpendAsync(
            businessId,
            monthsBack,
            cancellationToken);

        var insightsTask = _analyticsService.GetTopInsightsAsync(
            businessId,
            5,
            cancellationToken);

        await Task.WhenAll(benchmarkTask, forecastTask, insightsTask);

        var response = await insightsTask;

        var insightDtos = response.Insights.ToList();

        var executiveSummary = await _executiveSummaryService.GenerateAsync(
            businessId,
            insightDtos,
            benchmarkTask.Result,
            forecastTask.Result,
            cancellationToken);

        return new IntelligenceResponse
        {
            BusinessId = businessId,
            GeneratedAtUtc = DateTime.UtcNow,
            ExecutiveSummary = executiveSummary,
            Benchmark = benchmarkTask.Result,
            Forecast = forecastTask.Result,
            Insights = insightDtos
        };
    }

    private static InsightDto MapToDto(Models.InsightRecord record)
    {
        return new InsightDto
        {
            BusinessId = record.BusinessId,
            Type = record.InsightType,
            Title = record.Title,
            Description = record.Description,
            Severity = record.Severity,
            EstimatedImpact = record.ImpactValue,
            ImpactLabel = record.ImpactLabel,
            Recommendation = record.Recommendation,
            ConfidenceScore = record.ConfidenceScore,
            CurrencyCode = record.CurrencyCode,
            Metrics = string.IsNullOrWhiteSpace(record.MetricsJson)
                ? null
                : JsonSerializer.Deserialize<Dictionary<string, object>>(record.MetricsJson),
            Score = 0m
        };
    }
}