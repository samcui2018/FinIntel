namespace FinancialIntelligence.Api.Models;

public sealed class BenchmarkComparison
{
    public string MetricKey { get; init; } = string.Empty;
    public decimal ActualValue { get; init; }

    public decimal? P10 { get; init; }
    public decimal? P25 { get; init; }
    public decimal? P50 { get; init; }
    public decimal? P75 { get; init; }
    public decimal? P90 { get; init; }
    public decimal? MeanValue { get; init; }

    public decimal? Percentile { get; init; }
    public string PositionLabel { get; init; } = string.Empty;

    public Guid BenchmarkProfileId { get; init; }
    public string CohortLabel { get; init; } = string.Empty;
    public string SourceType { get; init; } = string.Empty;
    public decimal ConfidenceScore { get; init; }
    public int SampleSize { get; init; }

    public bool HasBenchmark =>
        P25.HasValue || P50.HasValue || P75.HasValue;

    public decimal EstimatePercentile(decimal value, decimal p25, decimal p50, decimal p75)
    {
        if (value <= p25)
            return 25m * (value / p25);

        if (value <= p50)
            return 25m + (value - p25) / (p50 - p25) * 25m;

        if (value <= p75)
            return 50m + (value - p50) / (p75 - p50) * 25m;

        return 75m + Math.Min(25m, (value - p75) / p75 * 25m);
    }

    public string GetPositionLabel(decimal percentile)
    {
        if (percentile < 25) return "below typical";
        if (percentile <= 75) return "within typical range";
        return "above typical";
    }
}
