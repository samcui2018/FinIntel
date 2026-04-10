namespace FinancialIntelligence.Api.Dtos.Analytics;

public sealed class InsightDto
{
    public Guid BusinessId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string VisualizationType { get; set; } = string.Empty;
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
    public InsightVisualizationDto? Visualization { get; set; }
}
public sealed class InsightVisualizationDto
{
    public string ChartType { get; set; } = string.Empty; // line, bar, comparison-bar
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public List<string> Labels { get; set; } = new();
    public List<InsightVisualizationSeriesDto> Series { get; set; } = new();
    public List<int> HighlightIndexes { get; set; } = new();
}

public sealed class InsightVisualizationSeriesDto
{
    public string Name { get; set; } = string.Empty;
    public List<decimal> Values { get; set; } = new();
}