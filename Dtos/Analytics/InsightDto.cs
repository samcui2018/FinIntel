namespace FinancialIntelligence.Api.Dtos.Analytics;

public sealed class InsightDto
{
    public Guid BusinessId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium";

    public decimal? EstimatedImpact { get; set; }
    public string? ImpactLabel { get; set; }

    public string? Recommendation { get; set; }
    public decimal? ConfidenceScore { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal Score { get; set; }

    public Dictionary<string, object>? Metrics { get; set; }
}