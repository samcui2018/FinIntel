namespace FinancialIntelligence.Api.Models;

public sealed class BenchmarkProfile
{
    public Guid BenchmarkProfileId { get; init; }
    public string ProfileName { get; init; } = string.Empty;
    public string? Industry { get; init; }
    public string? SpendBand { get; init; }
    public string? TransactionBand { get; init; }
    public string? Geography { get; init; }
    public string SourceType { get; init; } = string.Empty;
    public decimal ConfidenceScore { get; init; }
    public DateTime EffectiveDate { get; init; }
    public int SampleSize { get; init; }
}