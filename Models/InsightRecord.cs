namespace FinancialIntelligence.Api.Models;

public class InsightRecord
{
    public Guid InsightId { get; set; }
    public Guid LoadId { get; set; }
    public Guid BusinessId { get; set; }

    public string InsightType { get; set; } = string.Empty;     // fee_leakage, anomaly, concentration_risk
    public string Severity { get; set; } = "Low";               // High, Medium, Low
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string? ImpactLabel { get; set; }                    // "$1,240/month"
    public decimal? ImpactValue { get; set; }                   // 1240.00
    public string CurrencyCode { get; set; } = "USD";   
    public string MetricsJson { get; set; } = string.Empty; // optional for future multi-currency support
    public string? Recommendation { get; set; }
    public decimal? ConfidenceScore { get; set; }               // optional for future AI logic
    public DateTime CreatedAtUtc { get; set; }
}