using FinancialIntelligence.Api.Dtos.Analytics;
using FinancialIntelligence.Api.Dtos.Intelligence;
using FinancialIntelligence.Api.Repositories;

namespace FinancialIntelligence.Api.Services.Intelligence;

public sealed class PredictionService : IPredictionService
{
    private readonly IAnalyticsRepository _analyticsRepository;

    public PredictionService(IAnalyticsRepository analyticsRepository)
    {
        _analyticsRepository = analyticsRepository;
    }

    public async Task<SpendForecastDto> ForecastMonthlySpendAsync(
        Guid businessId,
        int monthsBack,
        CancellationToken cancellationToken = default)
    {
        var monthly = await _analyticsRepository.GetMonthlySpendAsync(
            businessId,
            monthsBack,
            cancellationToken);
        
        if (monthly.Count < 3)
        {
            return new SpendForecastDto
            {
                HasSufficientHistory = false,
                NextMonthForecast = 0m,
                TrendSlope = 0m,
                CurrencyCode = monthly.FirstOrDefault()?.CurrencyCode ?? "USD"
            };
        }

        var ordered = monthly.OrderBy(x => x.MonthStart).ToList();

        var slope = CalculateLinearSlope(ordered);
        var nextForecast = ordered[^1].Amount + slope;

        if (nextForecast < 0m)
        {
            nextForecast = 0m;
        }

        return new SpendForecastDto
        {
            HasSufficientHistory = true,
            NextMonthForecast = Math.Round(nextForecast, 2),
            TrendSlope = Math.Round(slope, 2),
            CurrencyCode = ordered[^1].CurrencyCode
        };
    }

    private static decimal CalculateLinearSlope(IReadOnlyList<MonthlySpendDto> monthly)
    {
        var n = monthly.Count;
        if (n < 2)
        {
            return 0m;
        }

        decimal sumX = 0m;
        decimal sumY = 0m;
        decimal sumXY = 0m;
        decimal sumXX = 0m;

        for (var i = 0; i < n; i++)
        {
            var x = (decimal)i;
            var y = monthly[i].Amount;

            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumXX += x * x;
        }

        var numerator = (n * sumXY) - (sumX * sumY);
        var denominator = (n * sumXX) - (sumX * sumX);

        if (denominator == 0m)
        {
            return 0m;
        }

        return numerator / denominator;
    }
}