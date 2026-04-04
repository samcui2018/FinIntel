namespace FinancialIntelligence.Api.Dtos.Analytics;

public sealed class InsightDto
{
    // public string Type { get; set; } = "";
    // public string Title { get; set; } = "";
    // public string Message { get; set; } = "";
    // public string Severity { get; set; } = "";
    // public decimal EstimatedImpact { get; set; }
    // public string CurrencyCode { get; set; } = "USD";
    // public Dictionary<string, object> Metrics { get; set; } = new();
    // public decimal Score { get; set; }

    // public string Description { get; set; } = "";
    // public string Recommendation { get; set; } = "";
    // public string ImpactLabel { get; set; } = "";
    // public decimal? ConfidenceScore { get; set; }
    // public Guid BusinessId { get; set; }
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