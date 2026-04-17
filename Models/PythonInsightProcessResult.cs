using System.Collections.Generic;
using System.Text.Json.Serialization;
using FinancialIntelligence.Api.Dtos.Analytics;

namespace FinancialIntelligence.Api.Models
{
    public sealed class PythonInsightProcessResult
    {
        [JsonPropertyName("insights")]
        public List<PythonInsightDto> Insights { get; set; } = new();

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}