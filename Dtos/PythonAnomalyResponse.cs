using System.Collections.Generic;
using System.Text.Json.Serialization;
namespace FinancialIntelligence.Api.Dtos.Analytics;
public sealed class PythonSpendAnomalyResponse
{
    public List<PythonInsightDto> Insights { get; set; } = new();
    public string? Error { get; set; }
}

// public sealed class PythonInsightDto
// {
//     public string Type { get; set; } = "";
//     public string Title { get; set; } = "";
//     public string Description { get; set; } = "";
//     public string Severity { get; set; } = "";
//     public decimal EstimatedImpact { get; set; }
//     public string Recommendation { get; set; } = "";
//     public decimal Score { get; set; }
//     public Dictionary<string, object> Metrics { get; set; } = new();
//     public string? VisualizationType { get; set; }
//     public PythonVisualizationDto? Visualization { get; set; }
// }
public sealed class PythonInsightDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = string.Empty;

        [JsonPropertyName("estimatedImpact")]
        public decimal EstimatedImpact { get; set; }

        [JsonPropertyName("recommendation")]
        public string Recommendation { get; set; } = string.Empty;

        [JsonPropertyName("score")]
        public decimal Score { get; set; }

        [JsonPropertyName("metrics")]
        public Dictionary<string, object>? Metrics { get; set; }

        [JsonPropertyName("visualizationType")]
        public string? VisualizationType { get; set; }

        [JsonPropertyName("visualization")]
        public PythonVisualizationDto? Visualization { get; set; }
    }
public sealed class PythonVisualizationDto
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("labels")]
    public List<string> Labels { get; set; } = new();

    [JsonPropertyName("highlightIndexes")]
    public List<int> HighlightIndexes { get; set; } = new();

    [JsonPropertyName("series")]
    public List<PythonVisualizationSeriesDto> Series { get; set; } = new();
}

public sealed class PythonVisualizationSeriesDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    [JsonPropertyName("values")]
    public List<decimal?> Values { get; set; } = new();
}