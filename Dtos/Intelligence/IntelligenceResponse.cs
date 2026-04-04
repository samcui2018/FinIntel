using FinancialIntelligence.Api.Dtos.Analytics;

namespace FinancialIntelligence.Api.Dtos.Intelligence;

public sealed class IntelligenceResponse
{
    public Guid BusinessId { get; set; }
    public DateTime GeneratedAtUtc { get; set; }

    public string ExecutiveSummary { get; set; } = string.Empty;

    public BenchmarkComparisonDto Benchmark { get; set; } = new();
    public SpendForecastDto Forecast { get; set; } = new();

    public IReadOnlyList<InsightDto> Insights { get; set; } = Array.Empty<InsightDto>();
}