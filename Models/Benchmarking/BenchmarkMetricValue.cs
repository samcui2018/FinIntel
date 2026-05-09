namespace FinancialIntelligence.Api.Models;

public sealed class BenchmarkMetricValue
{
    public Guid BenchmarkProfileId { get; init; }
    public string MetricKey { get; init; } = string.Empty;

    public decimal? P10 { get; init; }
    public decimal? P25 { get; init; }
    public decimal? P50 { get; init; }
    public decimal? P75 { get; init; }
    public decimal? P90 { get; init; }
    public decimal? MeanValue { get; init; }

    public int SampleSize { get; init; }
    public string? SourceNote { get; init; }
}