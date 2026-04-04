using FinancialIntelligence.Api.Models;
using FinancialIntelligence.Api.Services.Intelligence;

namespace FinancialIntelligence.Api.Services.Insights;

public sealed class PredictionInsightGenerator : IInsightGenerator
{
    private readonly IPredictionService _predictionService;

    public PredictionInsightGenerator(IPredictionService predictionService)
    {
        _predictionService = predictionService;
    }

    public async Task<IReadOnlyList<InsightRecord>> GenerateAsync(
        Guid loadId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"predictionGenerator called for businessId: {businessId}");
        var forecast = await _predictionService.ForecastMonthlySpendAsync(
            businessId,
            monthsBack: 6,
            cancellationToken);

        if (!forecast.HasSufficientHistory)
        {
            return Array.Empty<InsightRecord>();
        }

        var insights = new List<InsightRecord>();

        if (forecast.TrendSlope > 0m)
        {
            insights.Add(new InsightRecord
            {
                InsightId = Guid.NewGuid(),
                BusinessId = businessId,
                InsightType = "Prediction",
                Title = "Spend is projected to increase next month",
                Description = $"Based on recent monthly trends, next month's spend is forecasted at {forecast.NextMonthForecast:C}.",
                Severity = forecast.TrendSlope >= 5000m ? "High" : "Medium",
                ImpactValue = forecast.NextMonthForecast,
                ImpactLabel = "Projected Cost Increase",
                Recommendation = "Review recurring and discretionary expenses now so you can respond before the increase materializes.",
                ConfidenceScore = 0.72m,
                CurrencyCode = forecast.CurrencyCode,
                MetricsJson = null,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        else if (forecast.TrendSlope < 0m)
        {
            insights.Add(new InsightRecord
            {
                InsightId = Guid.NewGuid(),
                BusinessId = businessId,
                InsightType = "Prediction",
                Title = "Spend is projected to decline next month",
                Description = $"Recent trends suggest next month's spend may decline to about {forecast.NextMonthForecast:C}.",
                Severity = "Low",
                ImpactValue = forecast.NextMonthForecast,
                ImpactLabel = "Projected Spend Reduction",
                Recommendation = "Confirm whether the decline reflects improved efficiency, seasonality, or missing business activity.",
                ConfidenceScore = 0.70m,
                CurrencyCode = forecast.CurrencyCode,
                MetricsJson = null,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        return insights;
    }
}