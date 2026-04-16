namespace FinancialIntelligence.Api.Dtos.Analytics;
public sealed class PythonSpendAnomalyResponse
{
    public List<PythonInsightDto> Insights { get; set; } = new();
    public string? Error { get; set; }
}

public sealed class PythonInsightDto
{
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Severity { get; set; } = "";
    public decimal EstimatedImpact { get; set; }
    public string Recommendation { get; set; } = "";
    public decimal Score { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
    public string? VisualizationType { get; set; }
    public PythonVisualizationDto? Visualization { get; set; }
}

public sealed class PythonVisualizationDto
{
    public string? Title { get; set; }
    public List<string> Labels { get; set; } = new();
    public List<int> HighlightIndexes { get; set; } = new();
    public List<PythonVisualizationSeriesDto> Series { get; set; } = new();
}

public sealed class PythonVisualizationSeriesDto
{
    public string Name { get; set; } = "";
    public List<decimal> Values { get; set; } = new();
}